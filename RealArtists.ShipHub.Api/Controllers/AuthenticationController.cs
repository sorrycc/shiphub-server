﻿namespace RealArtists.ShipHub.Api.Controllers {
  using System;
  using System.Collections.Generic;
  using System.Data.Entity;
  using System.Linq;
  using System.Net;
  using System.Security.Cryptography;
  using System.Threading.Tasks;
  using System.Web.Http;
  using DataModel;
  using Models;
  using Utilities;

  [AllowAnonymous]
  [RoutePrefix("api/authentication")]
  public class AuthenticationController : ShipHubController {
    private static readonly IReadOnlyList<string> _requiredOauthScopes = new List<string>() {
      "user:email",
      "repo",
      "admin:repo_hook",
      "read:org",
      "admin:org_hook",
    }.AsReadOnly();

    [HttpGet]
    [Route("begin")]
    public IHttpActionResult Begin(string clientId = "dc74f7ec664b73a51971") {
      if (!GitHubSettings.Credentials.ContainsKey(clientId)) {
        return Error($"Unknown applicationId: {clientId}", HttpStatusCode.NotFound);
      }

      var secret = GitHubSettings.Credentials[clientId];
      var scope = string.Join(",", _requiredOauthScopes);
      var redir = new Uri(Request.RequestUri, Url.Link("callback", new { clientId = clientId })).ToString();
      string uri = $"https://github.com/login/oauth/authorize?client_id={clientId}&scope={scope}&redirect_uri={WebUtility.UrlEncode(redir)}";

      return Redirect(uri);
    }

    [HttpGet]
    [Route("end", Name = "callback")]
    public async Task<IHttpActionResult> End(string clientId, string code, string state = null) {
      if (!GitHubSettings.Credentials.ContainsKey(clientId)) {
        return Error($"Unknown applicationId: {clientId}", HttpStatusCode.NotFound);
      }

      return await Code(new CodeRequest() {
        ApplicationId = clientId,
        ClientName = "Loopback",
        Code = code,
        State = state
      });
    }

    public class CodeRequest {
      public string ApplicationId { get; set; }
      public string Code { get; set; }
      public string State { get; set; }
      public string ClientName { get; set; }
    }

    // Maybe used by web signup flow. If not, kill it.
    [HttpPost]
    [Route("code")]
    public async Task<IHttpActionResult> Code([FromBody] CodeRequest request) {
      if (string.IsNullOrWhiteSpace(request.ApplicationId)) {
        return BadRequest($"{nameof(request.ApplicationId)} is required.");
      }
      if (string.IsNullOrWhiteSpace(request.Code)) {
        return BadRequest($"{nameof(request.Code)} is required.");
      }
      if (string.IsNullOrWhiteSpace(request.ClientName)) {
        return BadRequest($"{nameof(request.ClientName)} is required.");
      }

      if (!GitHubSettings.Credentials.ContainsKey(request.ApplicationId)) {
        return Error($"Unknown applicationId: {request.ApplicationId}", HttpStatusCode.NotFound);
      }
      var secret = GitHubSettings.Credentials[request.ApplicationId];

      var appClient = GitHubSettings.CreateClient();
      var tokenInfo = await appClient.CreateAccessToken(request.ApplicationId, secret, request.Code, request.State);
      if (tokenInfo.Error != null) {
        return Error("Unable to retrieve token.", tokenInfo.Status, tokenInfo.Error);
      }
      var appAuth = tokenInfo.Result;

      return await Hello(new HelloRequest() {
        AccessToken = appAuth.AccessToken,
        ApplicationId = request.ApplicationId,
        ClientName = request.ClientName,
      });
    }

    public class HelloRequest {
      public string AccessToken { get; set; }
      public string ApplicationId { get; set; }
      public string ClientName { get; set; }
    }

    [HttpPost]
    [Route("hello")]
    public async Task<IHttpActionResult> Hello([FromBody] HelloRequest request) {
      if (string.IsNullOrWhiteSpace(request.ApplicationId)) {
        return BadRequest($"{nameof(request.ApplicationId)} is required.");
      }
      if (string.IsNullOrWhiteSpace(request.AccessToken)) {
        return BadRequest($"{nameof(request.AccessToken)} is required.");
      }
      if (string.IsNullOrWhiteSpace(request.ClientName)) {
        return BadRequest($"{nameof(request.ClientName)} is required.");
      }

      if (!GitHubSettings.Credentials.ContainsKey(request.ApplicationId)) {
        return Error($"Unknown applicationId: {request.ApplicationId}", HttpStatusCode.NotFound);
      }

      // Lookup Token
      GitHub.Models.Authorization tokenInfo = null;
      using (var appClient = GitHubSettings.CreateApplicationClient(request.ApplicationId)) {
        var checkTokenResponse = await appClient.CheckAccessToken(request.ApplicationId, request.AccessToken);
        if (checkTokenResponse.IsError) {
          return Error("Token validation failed.", HttpStatusCode.Unauthorized, checkTokenResponse.Error);
        }
        tokenInfo = checkTokenResponse.Result;

        // Check scopes
        var missingScopes = _requiredOauthScopes.Except(tokenInfo.Scopes).ToArray();
        if (missingScopes.Any()) {
          return Error("Insufficient scopes granted.", HttpStatusCode.Unauthorized, new {
            Granted = tokenInfo.Scopes,
            Missing = missingScopes,
          });
        }
      }

      using (var userClient = GitHubSettings.CreateUserClient(tokenInfo.Token)) {
        // DO NOT SEND ANY OPTIONS - we want to ensure we use the default credentials.
        var userResponse = await userClient.AuthenticatedUser();

        if (userResponse.IsError) {
          Error("Unable to determine current user.", HttpStatusCode.InternalServerError, userResponse.Error);
        }

        var userInfo = userResponse.Result;
        var user = await Context.Users
          .Include(x => x.AccessTokens)
          .Include(x => x.MetaData)
          .Include(x => x.Organizations)
          .SingleOrDefaultAsync(x => x.Id == userInfo.Id);
        if (user == null) {
          user = (User)Context.Accounts.Add(new User() {
            Id = userInfo.Id,
          });
        }
        Mapper.Map(userInfo, user);

        // CLEAR ANY METADATA - accounts are refreshed at a different endpoint, and eTags won't match, even if token does.
        if (user.MetaData != null) {
          Context.GitHubMetaData.Remove(user.MetaData);
          user.MetaData = null;
        }

        var token = user.AccessTokens.SingleOrDefault(x => x.Token == tokenInfo.Token);
        if (token == null) {
          // Map this ourselves here since this is the only place it's done.
          token = Context.AccessTokens.Add(new AccessToken() {
            Account = user,
            Token = tokenInfo.Token,
            ApplicationId = request.ApplicationId,
            CreatedAt = tokenInfo.CreatedAt,
          });
        }
        token.Scopes = string.Join(",", tokenInfo.Scopes);
        token.UpdateRateLimits(userResponse);

        // Always issues a new token. Maybe collect them later?
        var shipToken = Context.AuthenticationTokens.Add(new AuthenticationToken() {
          Token = new Guid(GetRandomBytes(16)),
          ClientName = request.ClientName,
          Account = user,
        });
        user.AuthenticationTokens.Add(shipToken);

        await Context.SaveChangesAsync();

        return Ok(new {
          Session = shipToken.Token,
          User = Mapper.Map<ApiUser>(user),
        });
      }
    }

    public static byte[] GetRandomBytes(int length) {
      var result = new byte[length];
      using (var rng = new RNGCryptoServiceProvider()) {
        rng.GetBytes(result);
      }
      return result;
    }
  }
}