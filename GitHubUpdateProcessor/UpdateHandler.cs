﻿namespace GitHubUpdateProcessor {
  using System;
  using System.Collections.Generic;
  using System.Data.Entity;
  using System.IO;
  using System.Linq;
  using System.Threading.Tasks;
  using AutoMapper;
  using Microsoft.Azure.WebJobs;
  using Newtonsoft.Json;
  using RealArtists.Ship.Server.QueueClient;
  using RealArtists.Ship.Server.QueueClient.Messages;
  using RealArtists.ShipHub.Common.DataModel;
  using gh = RealArtists.ShipHub.Common.GitHub.Models;

  /// <summary>
  /// It sucks that this class exists.
  /// 
  /// GitHub doesn't have a way to subscribe to all the notifications we want, so we
  /// have to poll. But we don't want to poll everything. And even the things we have
  /// to poll, we don't want to poll frequently.
  /// 
  /// When we poll and get complete data for one resource, we often learn about other
  /// resources as a side effect. This bonus information may already be known, but
  /// it can also be new or hint that we should poll a related item sooner than we
  /// had planned.
  /// 
  /// So we send all bonus information here. We may discard it, create stubs, and/or
  /// trigger an immediate complete update.
  /// 
  /// If possible, this class should never trigger notifications. Only the complete
  /// updates should do that.
  /// </summary>
  public static class UpdateHandler {
    // TODO: Locking?
    // TODO: Notifications

    /* TODO generally:
     * If updated with token and cache metadata is available, record it.
     * If not, wipe it.
     * When updated via a webhook, update last seen for the hook, and mark the item as refreshed by the hook.
     * Notifications if changed
     */

    public static IMapper Mapper { get; private set; }

    static UpdateHandler() {
      var config = new MapperConfiguration(cfg => {
        cfg.AddProfile<GitHubToDataModelProfile>();
        //cfg.AddProfile<DataModelToApiModelProfile>();
      });
      Mapper = config.CreateMapper();
    }

    public static async Task UpdateAccount(
      [ServiceBusTrigger(ShipHubQueueNames.UpdateAccount)] UpdateMessage<gh.Account> message,
      TextWriter logger) {
      using (var context = new ShipHubContext()) {
        await UpdateOrStubAccount(context, message.Value, message.ResponseDate);

        if (context.ChangeTracker.HasChanges()) {
          await context.SaveChangesAsync();
        }
      }
    }

    public static async Task UpdateRepository(
      [ServiceBusTrigger(ShipHubQueueNames.UpdateRepository)] UpdateMessage<gh.Repository> message,
      TextWriter logger) {
      using (var context = new ShipHubContext()) {
        await UpdateOrStubRepository(context, message.Value, message.ResponseDate);

        if (context.ChangeTracker.HasChanges()) {
          await context.SaveChangesAsync();
        }
      }
    }

    public static async Task UpdateRepositoryAssignable(
      [ServiceBusTrigger(ShipHubQueueNames.UpdateRepositoryAssignable)] UpdateMessage<RepositoryAssignableMessage> message,
      TextWriter logger) {
      var update = message.Value;
      using (var context = new ShipHubContext()) {
        var repo = await UpdateOrStubRepository(context, message.Value.Repository, message.ResponseDate);

        // Ensure repo and owner are saved if new.
        if (context.ChangeTracker.HasChanges()) {
          await context.SaveChangesAsync();
        }

        // Bulk update assignable users in a single shot.
        await context.UpdateRepositoryAssignableAccounts(
          repo.Id,
          update.AssignableAccounts.Select(x => new AccountStubTableRow() {
            AccountId = x.Id,
            Type = x.Type == gh.GitHubAccountType.Organization ? Account.OrganizationType : Account.UserType,
            Login = x.Login,
          }));
      }
    }

    [NoAutomaticTrigger]
    private static async Task<Account> UpdateOrStubAccount(ShipHubContext context, gh.Account account, DateTimeOffset responseDate) {
      var existing = await context.Accounts.SingleOrDefaultAsync(x => x.Id == account.Id);

      if (existing == null) {
        if (account.Type == gh.GitHubAccountType.Organization) {
          existing = context.Accounts.Add(new Organization());
        } else {
          existing = context.Accounts.Add(new User());
        }
        existing.Id = account.Id;
      }

      // This works for new additions because DatetTimeOffset defaults to its minimum value.
      if (existing.Date < responseDate) {
        Mapper.Map(account, existing);

        // TODO: Gross
        var trackingState = context.ChangeTracker.Entries<Account>().Single(x => x.Entity.Id == account.Id);
        if (trackingState.State != EntityState.Unchanged) {
          existing.Date = responseDate;
        }
      }

      return existing;
    }

    [NoAutomaticTrigger]
    private static async Task<Repository> UpdateOrStubRepository(ShipHubContext context, gh.Repository repo, DateTimeOffset responseDate) {
      var owner = await UpdateOrStubAccount(context, repo.Owner, responseDate);
      var existing = await context.Repositories.SingleOrDefaultAsync(x => x.Id == repo.Id);

      if (existing == null) {
        existing = context.Repositories.Add(new Repository() {
          Id = repo.Id,
        });
      }

      existing.Account = owner;
      Mapper.Map(repo, existing);

      return existing;
    }
  }
}