// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Exceptions;
using BaseExceptionHandler = Voting.Lib.Rest.Middleware.ExceptionHandler;

namespace Voting.Ausmittlung.Middlewares;

public class ExceptionHandler : BaseExceptionHandler
{
    public ExceptionHandler(PublisherConfig config, RequestDelegate next, ILogger<ExceptionHandler> logger)
        : base(next, logger, config.EnableDetailedErrors)
    {
    }

    protected override int MapExceptionToStatus(Exception ex) => ExceptionMapping.MapToHttpStatusCode(ex);

    protected override bool ExposeExceptionType(Exception ex) => ExceptionMapping.ExposeExceptionType(ex);
}
