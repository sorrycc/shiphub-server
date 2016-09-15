﻿namespace RealArtists.ShipHub.Api.Sync.Messages.Entries {
  using System;
  using System.Runtime.Serialization;

  public enum SubscriptionMode {
    [EnumMember(Value = "paid")]
    Paid = 0,

    [EnumMember(Value = "trial")]
    Trial,

    [EnumMember(Value = "free")]
    Free
  }

  public class SubscriptionEntry : SyncEntity {
    public SubscriptionMode Mode { get; set; }
    public DateTimeOffset? TrialEndDate { get; set; }
  }
}