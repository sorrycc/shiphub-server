﻿namespace RealArtists.ShipHub.QueueProcessor {
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using Common;
  using Common.DataModel;
  using Common.DataModel.Types;
  using Microsoft.Azure.WebJobs;
  using QueueClient;
  using QueueClient.Messages;

  public static class SyncHandler {
    /// <summary>
    /// Precondition: None.
    /// Postcondition: User saved in DB.
    /// </summary>
    public static async Task SyncAccount(
      [ServiceBusTrigger(ShipHubQueueNames.SyncAccount)] AccessTokenMessage message,
      [ServiceBus(ShipHubQueueNames.SyncAccountRepositories)] IAsyncCollector<AccountMessage> syncAccountRepos,
      [ServiceBus(ShipHubQueueNames.SyncAccountOrganizations)] IAsyncCollector<AccountMessage> syncAccountOrgs) {
      var ghc = GitHubSettings.CreateUserClient(message.AccessToken);

      var userResponse = await ghc.User();
      var user = userResponse.Result;
      //await UpdateHandler.UpdateAccount(new UpdateMessage<gh.Account>(user, userResponse.Date, userResponse.CacheData));
      using (var context = new ShipHubContext()) {
        await context.BulkUpdateAccounts(userResponse.Date, new[] { SharedMapper.Map<AccountTableType>(user) });
      }

      // Now that the user is saved in the DB, safe to sync all repos and user's orgs
      var am = new AccountMessage() {
        AccessToken = message.AccessToken,
        Account = user,
      };

      await Task.WhenAll(
        syncAccountRepos.AddAsync(am),
        syncAccountOrgs.AddAsync(am));
    }

    /// <summary>
    /// Precondition: User saved in DB.
    /// Postcondition: User's repos, their owners, and user's repo-links saved in DB.
    /// </summary>
    public static async Task SyncAccountRepositories(
      [ServiceBusTrigger(ShipHubQueueNames.SyncAccountRepositories)] AccountMessage message,
      [ServiceBus(ShipHubQueueNames.SyncRepository)] IAsyncCollector<RepositoryMessage> syncRepo) {
      var ghc = GitHubSettings.CreateUserClient(message.AccessToken);

      var repoResponse = await ghc.Repositories();
      var reposWithIssues = repoResponse.Result.Where(x => x.HasIssues);
      var assignableRepos = reposWithIssues.ToDictionary(x => x.FullName, x => ghc.Assignable(x.FullName, message.Account.Login));
      await Task.WhenAll(assignableRepos.Values.ToArray());
      var keepRepos = reposWithIssues.Where(x => assignableRepos[x.FullName].Result.Result).ToArray();

      using (var context = new ShipHubContext()) {
        var owners = keepRepos
          .Select(x => x.Owner)
          .GroupBy(x => x.Login)
          .Select(x => x.First());
        await context.BulkUpdateAccounts(repoResponse.Date, SharedMapper.Map<IEnumerable<AccountTableType>>(owners));
        await context.BulkUpdateRepositories(repoResponse.Date, SharedMapper.Map<IEnumerable<RepositoryTableType>>(keepRepos));
        await context.SetAccountLinkedRepositories(message.Account.Id, keepRepos.Select(x => x.Id));
      }

      // Now that owners, repos, and links are saved, safe to sync the repos themselves.
      var syncTasks = keepRepos.Select(x => syncRepo.AddAsync(new RepositoryMessage() {
        AccessToken = message.AccessToken,
        Repository = x,
      })).ToArray();

      await Task.WhenAll(syncTasks);
    }

    ///
    /// NOTE WELL: We sync only sync orgs for which the user is a member. If they can see a repo in an org
    /// but aren't a member, too bad for them. The permissions are too painful otherwise.
    ///

    /// <summary>
    /// Syncs the list of organizations of which the account is a member.
    /// Precondition: User exists
    /// Postcondition: User's organizations exist
    /// </summary>
    public static async Task SyncAccountOrganizations(
      [ServiceBusTrigger(ShipHubQueueNames.SyncAccountOrganizations)] AccountMessage message,
      [ServiceBus(ShipHubQueueNames.SyncOrganizationMembers)] IAsyncCollector<AccountMessage> syncOrgMembers) {
      var ghc = GitHubSettings.CreateUserClient(message.AccessToken);

      var orgResponse = await ghc.Organizations();
      var orgs = orgResponse.Result;

      using (var context = new ShipHubContext()) {
        await context.BulkUpdateAccounts(orgResponse.Date, SharedMapper.Map<IEnumerable<AccountTableType>>(orgs));
        await context.SetUserOrganizations(message.Account.Id, orgs.Select(x => x.Id));
      }

      var memberSyncMessages = orgs.
        Select(x => syncOrgMembers.AddAsync(new AccountMessage() {
          AccessToken = message.AccessToken,
          Account = x,
        })).ToArray();
      await Task.WhenAll(memberSyncMessages);
    }

    /// <summary>
    /// Precondition: Organizations exist.
    /// Postcondition: Org members exist and Org membership is up to date.
    /// </summary>
    public static async Task SyncOrganizationMembers([ServiceBusTrigger(ShipHubQueueNames.SyncOrganizationMembers)] AccountMessage message) {
      var ghc = GitHubSettings.CreateUserClient(message.AccessToken);

      var memberResponse = await ghc.OrganizationMembers(message.Account.Login);
      var members = memberResponse.Result;

      using (var context = new ShipHubContext()) {
        await context.BulkUpdateAccounts(memberResponse.Date, SharedMapper.Map<IEnumerable<AccountTableType>>(members));
        await context.SetOrganizationUsers(message.Account.Id, members.Select(x => x.Id));
      }
    }

    /// <summary>
    /// Precondition: Repos saved in DB
    /// Postcondition: None.
    /// </summary>
    //public static async Task SyncRepository(
    //  [ServiceBusTrigger(ShipHubQueueNames.SyncRepository)] RepositoryMessage message,
    //  [ServiceBus(ShipHubQueueNames.SyncRepositoryAssignees)] IAsyncCollector<RepositoryMessage> syncRepoAssignees,
    //  [ServiceBus(ShipHubQueueNames.SyncRepositoryAssignees)] IAsyncCollector<RepositoryMessage> syncRepoAssignees,
    //  [ServiceBus(ShipHubQueueNames.SyncRepositoryAssignees)] IAsyncCollector<RepositoryMessage> syncRepoAssignees,
    //  [ServiceBus(ShipHubQueueNames.SyncRepositoryAssignees)] IAsyncCollector<RepositoryMessage> syncRepoAssignees,
    //  [ServiceBus(ShipHubQueueNames.SyncRepositoryIssues)] IAsyncCollector<RepositoryMessage> syncRepoIssues) {

    //  await Task.CompletedTask;
    //}
  }
}
