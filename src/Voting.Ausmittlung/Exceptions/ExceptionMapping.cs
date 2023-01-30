// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Schema;
using AutoMapper;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Lib.Eventing.Exceptions;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.VotingExports.Exceptions;

namespace Voting.Ausmittlung.Exceptions;

internal readonly struct ExceptionMapping
{
    private const string EnumMappingErrorSource = "AutoMapper.Extensions.EnumMapping";
    private readonly StatusCode _grpcStatusCode;
    private readonly int _httpStatusCode;
    private readonly bool _exposeExceptionType;

    public ExceptionMapping(StatusCode grpcStatusCode, int httpStatusCode, bool exposeExceptionType = false)
    {
        _grpcStatusCode = grpcStatusCode;
        _httpStatusCode = httpStatusCode;
        _exposeExceptionType = exposeExceptionType;
    }

    public static int MapToHttpStatusCode(Exception ex)
        => Map(ex)._httpStatusCode;

    public static StatusCode MapToGrpcStatusCode(Exception ex)
        => Map(ex)._grpcStatusCode;

    public static bool ExposeExceptionType(Exception ex)
        => Map(ex)._exposeExceptionType;

    private static ExceptionMapping Map(Exception ex)
        => ex switch
        {
            EVotingNotActiveException _ => new ExceptionMapping(StatusCode.FailedPrecondition, StatusCodes.Status424FailedDependency, true),
            CountingCircleResultInInvalidStateForEVotingImportException _ => new ExceptionMapping(StatusCode.FailedPrecondition, StatusCodes.Status424FailedDependency, true),
            SecondFactorTransactionDataChangedException _ => new ExceptionMapping(StatusCode.FailedPrecondition, StatusCodes.Status424FailedDependency, true),
            SecondFactorTransactionNotVerifiedException _ => new ExceptionMapping(StatusCode.FailedPrecondition, StatusCodes.Status424FailedDependency, true),
            VerifySecondFactorTimeoutException _ => new ExceptionMapping(StatusCode.FailedPrecondition, StatusCodes.Status424FailedDependency, true),
            NotAuthenticatedException _ => new ExceptionMapping(StatusCode.Unauthenticated, StatusCodes.Status401Unauthorized),
            ForbiddenException _ => new ExceptionMapping(StatusCode.PermissionDenied, StatusCodes.Status403Forbidden),
            FluentValidation.ValidationException _ => new ExceptionMapping(StatusCode.InvalidArgument, StatusCodes.Status400BadRequest),
            EntityNotFoundException _ => new ExceptionMapping(StatusCode.NotFound, StatusCodes.Status404NotFound),
            Report.Exceptions.EntityNotFoundException _ => new ExceptionMapping(StatusCode.NotFound, StatusCodes.Status404NotFound),
            ContestLockedException _ => new ExceptionMapping(StatusCode.FailedPrecondition, StatusCodes.Status400BadRequest),
            ContestTestingPhaseEndedException _ => new ExceptionMapping(StatusCode.FailedPrecondition, StatusCodes.Status400BadRequest),
            AggregateNotFoundException _ => new ExceptionMapping(StatusCode.NotFound, StatusCodes.Status404NotFound),
            VersionMismatchException _ => new ExceptionMapping(StatusCode.Aborted, StatusCodes.Status424FailedDependency),
            AggregateDeletedException _ => new ExceptionMapping(StatusCode.NotFound, StatusCodes.Status404NotFound),
            TemplateNotFoundException _ => new ExceptionMapping(StatusCode.NotFound, StatusCodes.Status404NotFound),
            AutoMapperMappingException autoMapperException when autoMapperException.InnerException is not null => Map(autoMapperException.InnerException),
            AutoMapperMappingException autoMapperException when string.Equals(autoMapperException.Source, EnumMappingErrorSource) => new(StatusCode.InvalidArgument, StatusCodes.Status400BadRequest),
            ValidationException _ => new ExceptionMapping(StatusCode.InvalidArgument, StatusCodes.Status400BadRequest),
            XmlSchemaValidationException => new(StatusCode.InvalidArgument, StatusCodes.Status400BadRequest),
            _ => new ExceptionMapping(StatusCode.Internal, StatusCodes.Status500InternalServerError),
        };
}
