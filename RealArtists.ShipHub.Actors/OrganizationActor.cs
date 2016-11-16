﻿namespace RealArtists.ShipHub.Actors {
  using System;
  using System.Collections.Generic;
  using System.Data.Entity;
  using System.Linq;
  using System.Net;
  using System.Threading.Tasks;
  using ActorInterfaces;
  using AutoMapper;
  using Common;
  using Common.DataModel;
  using Common.DataModel.Types;
  using GitHub;
  using Orleans;
  using QueueClient;
  using gh = Common.GitHub.Models;

  public class OrganizationActor : Grain, IOrganizationActor {
    public static readonly TimeSpan SyncDelay = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan SyncIdle = TimeSpan.FromSeconds(SyncDelay.TotalSeconds * 3);

    private IMapper _mapper;
    private IGrainFactory _grainFactory;
    private IFactory<ShipHubContext> _contextFactory;
    private IShipHubQueueClient _queueClient;

    private long _orgId;
    private string _login;

    // Metadata
    private GitHubMetadata _metadata;
    private GitHubMetadata _adminMetadata;
    private GitHubMetadata _memberMetadata;

    // Data
    private IEnumerable<gh.Account> _members = Array.Empty<gh.Account>();
    private IEnumerable<gh.Account> _admins = Array.Empty<gh.Account>();

    // Sync logic
    private DateTimeOffset _lastSyncInterest;
    private IDisposable _syncTimer;
    private Random _random = new Random();

    public OrganizationActor(IMapper mapper, IGrainFactory grainFactory, IFactory<ShipHubContext> contextFactory, IShipHubQueueClient queueClient) {
      _mapper = mapper;
      _grainFactory = grainFactory;
      _contextFactory = contextFactory;
      _queueClient = queueClient;
    }

    public override async Task OnActivateAsync() {
      using (var context = _contextFactory.CreateInstance()) {
        _orgId = this.GetPrimaryKeyLong();

        // Ensure this organization actually exists
        var org = await context.Organizations.SingleOrDefaultAsync(x => x.Id == _orgId);

        if (org == null) {
          throw new InvalidOperationException($"Organization {_orgId} does not exist and cannot be activated.");
        }

        _login = org.Login;
        _metadata = org.OrganizationMetadata;

        // TODO: Load member and admin metadata
        // For now, we just start them null and request the data once.
        // Be sure to load the current membership/admin data too if you load
        // the metadata in the future. It's assumed to be populated when
        // changes are detected.
      }

      await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync() {
      using (var context = _contextFactory.CreateInstance()) {
        // I think all we need to persist is the metadata.
        await context.UpdateMetadata("Accounts", "OrgMetadataJson", _orgId, _metadata);
        // TODO: Persist admin and member metadata
      }

      // TODO: Look into how agressively Orleans deactivates "inactive" grains.
      // We may need to delay deactivation based on sync interest.

      await base.OnDeactivateAsync();
    }

    public Task Sync(long forUserId) {
      // For now, calls to sync just indicate interest in syncing.
      // Rather than sync here, we just ensure that a timer is registered.
      _lastSyncInterest = DateTimeOffset.UtcNow;

      if (_syncTimer == null) {
        _syncTimer = RegisterTimer(SyncCallback, null, TimeSpan.Zero, SyncDelay);
      }

      return Task.CompletedTask;
    }

    private async Task SyncCallback(object state) {
      if (DateTimeOffset.UtcNow.Subtract(_lastSyncInterest) > SyncIdle) {
        DeactivateOnIdle();
        return;
      }

      var tasks = new List<Task>();
      var changes = new ChangeSummary();
      using (var context = _contextFactory.CreateInstance()) {
        var syncUserIds = await context.OrganizationAccounts
          .Where(x => x.OrganizationId == _orgId)
          .Where(x => x.User.Token != null)
          .Select(x => x.UserId)
          .ToArrayAsync();

        if (syncUserIds.Length == 0) {
          DeactivateOnIdle();
          return;
        }

        var github = new GitHubActorPool(_grainFactory, syncUserIds);

        // Org itself
        if (_metadata == null || _metadata.Expires < DateTimeOffset.UtcNow) {
          var org = await github.Organization(_login, _metadata);

          if (org.Status != HttpStatusCode.NotModified) {
            changes.UnionWith(
              await context.UpdateAccount(org.Date, _mapper.Map<AccountTableType>(org.Result))
            );
          }

          // Don't update until saved.
          _metadata = GitHubMetadata.FromResponse(org);
        }

        if (_memberMetadata == null || _memberMetadata.Expires < DateTimeOffset.UtcNow) {
          // GitHub's `/orgs/<name>/members` endpoint does not provide role info for
          // each member.  To workaround, we make two requests and use the filter option
          // to only get admins or non-admins on each request.

          var updated = false;

          var members = await github.OrganizationMembers(_login, role: "member", cacheOptions: _memberMetadata);
          if (members.Status != HttpStatusCode.NotModified) {
            updated = true;
            _members = members.Result;
          }

          var admins = await github.OrganizationMembers(_login, role: "admin", cacheOptions: _adminMetadata);
          if (admins.Status != HttpStatusCode.NotModified) {
            updated = true;
            _admins = admins.Result;
          }

          if (updated) {
            changes.UnionWith(
              await context.BulkUpdateAccounts(
                members.Date,
                _mapper.Map<IEnumerable<AccountTableType>>(_members.Concat(_admins))));

            var orgMemberChanges = await context.SetOrganizationUsers(
                _orgId,
                _members
                  .Select(x => Tuple.Create(x.Id, false))
                  .Concat(_admins.Select(x => Tuple.Create(x.Id, true))));

            if (!orgMemberChanges.Empty) {
              // Check for subscription changes
              var subscription = await context.Subscriptions.SingleOrDefaultAsync(x => x.AccountId == _orgId);

              if (subscription?.State == SubscriptionState.Subscribed) {
                // If you belong to a paid organization, your personal subscription
                // is complimentary.  We need to add or remove the coupon for this
                // as membership changes.
                tasks.AddRange(orgMemberChanges.Users.Select(x => _queueClient.BillingUpdateComplimentarySubscription(x)));
              }
            }
            changes.UnionWith(orgMemberChanges);
          }

          _memberMetadata = GitHubMetadata.FromResponse(members);
          _adminMetadata = GitHubMetadata.FromResponse(admins);
        }

        // If any active org members are admins, update webhooks
        // TODO: Does this really need to happen this often?
        var activeAdmins = await context.OrganizationAccounts
          .Where(x => x.OrganizationId == _orgId
            && x.Admin == true
            && x.User.Token != null)
          .Select(x => x.UserId)
          .ToArrayAsync();

        if (activeAdmins.Any()) {
          var randmin = activeAdmins[_random.Next(activeAdmins.Length)];
          tasks.Add(_queueClient.AddOrUpdateOrgWebhooks(_orgId, randmin));
        }
      }

      // Send Changes.
      if (!changes.Empty) {
        tasks.Add(_queueClient.NotifyChanges(changes));
      }

      // Await all outstanding operations.
      await Task.WhenAll(tasks);
    }
  }
}