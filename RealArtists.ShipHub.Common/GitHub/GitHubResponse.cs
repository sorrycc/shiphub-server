﻿namespace RealArtists.ShipHub.Common.GitHub {
  using System;
  using System.Net;

  public class GitHubResponse {
    public Uri RequestUri { get; set; }
    public HttpStatusCode Status { get; set; }

    public bool IsError { get; set; }
    public GitHubError Error { get; set; }
    public GitHubRedirect Redirect { get; set; }
    public GitHubPagination Pagination { get; set; }

    public GitHubCacheData CacheData { get; set; }
    public GitHubRateLimit RateLimit { get; set; }
  }

  public class GitHubResponse<T> : GitHubResponse {
    private bool _resultSet = false;
    private T _result = default(T);
    public T Result {
      get {
        if (IsError) {
          throw new InvalidOperationException("Cannot get the result of failed request.");
        }

        if (!_resultSet) {
          throw new InvalidOperationException("Cannot get the result before it's set.");
        }

        return _result;
      }
      set {
        // Allow results to be set multiple times because I'm lazy and pagination uses it.
        if (IsError) {
          throw new InvalidOperationException("Cannot set the result of failed request.");
        }

        _result = value;
        _resultSet = true;
      }
    }
  }
}
