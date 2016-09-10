﻿namespace RealArtists.ShipHub.Api.Controllers {
  using System.Net;
  using System.Runtime.CompilerServices;
  using System.Web.Http;
  using Common.DataModel;
  using Filters;

  public abstract class ShipHubController : ApiController {
    protected ShipHubContext Context { get; private set; } = new ShipHubContext();
    protected ShipHubPrincipal ShipHubUser => RequestContext.Principal as ShipHubPrincipal;

    public IHttpActionResult Error(
                         string message,
                         HttpStatusCode status = HttpStatusCode.InternalServerError,
                         object details = null,
      [CallerMemberName] string memberName = "",
      [CallerFilePath]   string sourceFilePath = "",
      [CallerLineNumber] int sourceLineNumber = 0) {

      var error = new {
        Error = new {
          Message = message,
          CallerMemberName = memberName,
          CallerLineNumber = sourceLineNumber,
          CallerFilePath = sourceFilePath,
          Details = details,
        },
      };

      return Content(status, error);
    }

    protected override void Dispose(bool disposing) {
      if (disposing) {
        if (Context != null) {
          Context.Dispose();
          Context = null;
        }
      }
      base.Dispose(disposing);
    }
  }
}
