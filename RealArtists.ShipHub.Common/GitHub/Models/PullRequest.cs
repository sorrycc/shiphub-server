﻿namespace RealArtists.ShipHub.Common.GitHub.Models {
  using System;
  using System.Collections.Generic;

  public class PullRequest : Issue {
    // A pull request (as of 2017-02-24) has all the same fields as an Issue save pull_request

    public string MergeCommitSha { get; set; }
    public DateTimeOffset? MergedAt { get; set; }
    public CommitReference Head { get; set; }
    public CommitReference Base { get; set; }

    // These aren't consistently sent 😒
    public int? Additions { get; set; }
    public int? ChangedFiles { get; set; }
    public int? Comments { get; set; }
    public int? Commits { get; set; }
    public int? Deletions { get; set; }
    public bool? MaintainerCanModify { get; set; }
    public bool? Mergeable { get; set; }
    public string MergeableState { get; set; }
    public bool? Merged { get; set; }
    public Account MergedBy { get; set; }
    public bool? Rebaseable { get; set; }
    public int? ReviewComments { get; set; }

    public IEnumerable<Account> RequestedReviewers { get; set; }
  }

  public class CommitReference {
    public string Label { get; set; }
    public string Ref { get; set; }
    public string Sha { get; set; }
    public Account User { get; set; }
    public Repository Repo { get; set; }
  }
}
