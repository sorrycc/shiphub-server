﻿namespace RealArtists.ShipHub.QueueProcessor.Jobs {
  using System;
  using System.Threading.Tasks;
  using Tracing;

  public abstract class LoggingHandlerBase {
    private IDetailedExceptionLogger _logger;

    protected LoggingHandlerBase(IDetailedExceptionLogger logger) {
      _logger = logger;
    }

    /// <summary>
    /// Yes, seriously, because the webjobs sdk has all interesting extensibilty points marked internal only. =(
    /// </summary>
    /// <param name="functionInstanceId">So we can look it up in the webjob logs.</param>
    /// <param name="forUserId">So we know (finally!) who was affected.</param>
    /// <param name="message">So we can replay the message that failed, and determine why.</param>
    /// <param name="action">The stuff to try to do.</param>
    public async Task WithEnhancedLogging(Guid functionInstanceId, long? forUserId, object message, Func<Task> action) {
      try {
        await action();
      } catch (Exception e) {
        _logger.Log(functionInstanceId, forUserId, message, e);
        throw new TraceBypassException("Exception already logged", e);
      }
    }
  }
}
