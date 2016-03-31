﻿namespace RealArtists.ShipHub.Api.DataModel {
  public interface IVersionedResource {
    string TopicName { get; }
    long RowVersion { get; set; }
  }
}
