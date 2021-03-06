﻿namespace RealArtists.ShipHub.Api.Sync.Messages {
  using System;
  using System.Collections.Generic;
  using System.Text.RegularExpressions;
  using Newtonsoft.Json;

  public class RepositoryVersion {
    [JsonProperty("repo")]
    public long Id { get; set; }
    public long Version { get; set; }
  }

  public class OrganizationVersion {
    [JsonProperty("org")]
    public long Id { get; set; }
    public long Version { get; set; }
  }

  public class VersionDetails {
    public VersionDetails() { }

    public VersionDetails(IEnumerable<RepositoryVersion> repoVersions, IEnumerable<OrganizationVersion> orgVersions, long pullRequestVersion, long mentionsVersion, long queriesVersion, long mergeRestrictionVersion) {
      Repositories = repoVersions;
      Organizations = orgVersions;
      PullRequestVersion = pullRequestVersion;
      MentionsVersion = mentionsVersion;
      QueriesVersion = queriesVersion;
      MergeRestrictionVersion = mergeRestrictionVersion;
    }

    [JsonProperty("repos")]
    public IEnumerable<RepositoryVersion> Repositories { get; set; }

    [JsonProperty("orgs")]
    public IEnumerable<OrganizationVersion> Organizations { get; set; }

    [JsonProperty("prv")]
    public long? PullRequestVersion { get; set; }

    [JsonProperty("mv")]
    public long? MentionsVersion { get; set; }

    [JsonProperty("q")]
    public long? QueriesVersion { get; set; }

    [JsonProperty("mrv")]
    public long? MergeRestrictionVersion { get; set; }
  }

  public class HelloRequest : SyncMessageBase {
    public VersionDetails Versions { get; set; }

    private string _rawClientVersionString;

    [JsonProperty("client")]
    public string RawClientVersionString {
      get => _rawClientVersionString;
      set {
        _rawClientVersionString = value;
        ParseClientVersion(value);
      }
    }

    // Helpers to parse client user agent string
    // Example Client: 
    // com.realartists.Ship2 2.0 (1), OS X 10.11.6

    private static readonly Regex VersionRegex = new Regex(
      @"^(?<ProductIdentifier>[^ ]*) " +
      @"(?<MarketingVersion>\d+(\.\d+){0,3}) " +
      @"\((?<BuildVersion>\d+(\.\d+){0,3})\), " +
      @"(?<ClientOS>(?<ClientOSName>.+?) (?<ClientOSVersion>\d+(\.\d+){0,3}))$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline,
      TimeSpan.FromMilliseconds(100));

    private static readonly Version _devBuildSentinel = new Version(1, 0);
    private static readonly Version _devBuildReplacement = new Version(int.MaxValue, 0);
    private void ParseClientVersion(string value) {
      var match = VersionRegex.Match(value);
      if (!match.Success) {
        throw new ArgumentException($"Version string '{value}' is not in a recognized format.", nameof(value));
      }
      ProductIdentifier = match.Groups["ProductIdentifier"].Value;
      MarketingVersion = ParseVersion(match.Groups["MarketingVersion"].Value);
      BuildVersion = ParseVersion(match.Groups["BuildVersion"].Value);
      ClientOS = match.Groups["ClientOS"].Value;
      ClientOSName = match.Groups["ClientOSName"].Value;
      ClientOSVersion = ParseVersion(match.Groups["ClientOSVersion"].Value);

      // Fixup for dev builds (which send build version "1")
      if (BuildVersion == _devBuildSentinel) {
        BuildVersion = _devBuildReplacement;
      }
    }

    private Version ParseVersion(string version) {
      if (!version.Contains(".")) {
        version += ".0";
      }
      return Version.Parse(version);
    }

    /// <summary>
    /// The product identifier of the client. Ex: com.realartists.Ship2
    /// </summary>
    [JsonIgnore]
    public string ProductIdentifier { get; private set; }

    /// <summary>
    /// The marketing version of the client. Ex: 2.0
    /// </summary>
    [JsonIgnore]
    public Version MarketingVersion { get; private set; }

    /// <summary>
    /// The build number of the client. Ex: 337. Returns int.MaxValue for development builds, which the client reports as (1).
    /// </summary>
    [JsonIgnore]
    public Version BuildVersion { get; private set; }

    /// <summary>
    /// The OS name and version of the client. Ex: OS X 10.11.6
    /// </summary>
    [JsonIgnore]
    public string ClientOS { get; private set; }

    /// <summary>
    /// The OS name of the client. Ex: OS X
    /// </summary>
    [JsonIgnore]
    public string ClientOSName { get; private set; }

    /// <summary>
    /// The OS version of the client. Ex: 10.11.6
    /// </summary>
    [JsonIgnore]
    public Version ClientOSVersion { get; private set; }
  }
}
