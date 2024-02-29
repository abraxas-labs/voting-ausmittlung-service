// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Exceptions;
using BaseExceptionInterceptor = Voting.Lib.Grpc.Interceptors.ExceptionInterceptor;

namespace Voting.Ausmittlung.Interceptors;

/// <summary>
/// Logs errors and sets mapped status codes.
/// Currently only implemented for async unary and async server streaming calls since no other call types are used (yet).
/// </summary>
public class ExceptionInterceptor : BaseExceptionInterceptor
{
    public ExceptionInterceptor(PublisherConfig config, ILogger<ExceptionInterceptor> logger)
        : base(logger, config.EnableDetailedErrors)
    {
    }

    protected override StatusCode MapExceptionToStatusCode(Exception ex)
        => ExceptionMapping.MapToGrpcStatusCode(ex);

    protected override bool ExposeExceptionType(Exception ex)
        => ExceptionMapping.ExposeExceptionType(ex);
}
