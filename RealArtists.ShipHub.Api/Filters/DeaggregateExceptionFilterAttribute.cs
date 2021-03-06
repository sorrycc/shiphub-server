﻿namespace RealArtists.ShipHub.Api.Filters {
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using System.Web.Http.Filters;
  using Common;

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
  public sealed class DeaggregateExceptionFilterAttribute : ExceptionFilterAttribute {
    public override void OnException(HttpActionExecutedContext actionExecutedContext) {
      actionExecutedContext.Exception = actionExecutedContext.Exception.Simplify();
      base.OnException(actionExecutedContext);
    }

    public override Task OnExceptionAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken) {
      actionExecutedContext.Exception = actionExecutedContext.Exception.Simplify();
      return base.OnExceptionAsync(actionExecutedContext, cancellationToken);
    }
  }
}
