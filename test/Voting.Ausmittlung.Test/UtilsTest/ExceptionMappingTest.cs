// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AutoMapper;
using FluentAssertions;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Exceptions;
using Voting.Lib.Eventing.Exceptions;
using Voting.Lib.Iam.Exceptions;
using Voting.Lib.VotingExports.Exceptions;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class ExceptionMappingTest
{
    private const string DefaultError = "error";

    [Fact]
    public void ShouldExposeExceptionType()
    {
        var exposedExceptions = new HashSet<Exception>()
        {
            new CountingCircleResultInInvalidStateForEVotingImportException(Guid.Empty),
            new EVotingNotActiveException(DefaultError, Guid.Empty),
            new SecondFactorTransactionDataChangedException(),
            new SecondFactorTransactionNotVerifiedException(),
            new VerifySecondFactorTimeoutException(),
            new SecondaryMajorityElectionCandidateNotSelectedInPrimaryElectionException(),
        };

        var hiddenExceptions = new HashSet<Exception>()
        {
            new Exception(),
            new NotAuthenticatedException(),
            new ForbiddenException(),
            new ValidationException(),
            new FluentValidation.ValidationException(DefaultError),
            new EntityNotFoundException(DefaultError),
            new Report.Exceptions.EntityNotFoundException(DefaultError, Guid.Empty),
            new ContestLockedException(),
            new AggregateNotFoundException(Guid.Empty),
            new AggregateDeletedException(Guid.Empty),
            new TemplateNotFoundException(DefaultError),
            new AutoMapperMappingException(DefaultError),
        };

        var shouldExposeExceptionTypeByException = new Dictionary<Type, (Exception Exception, bool ShouldExposeExceptionType)?>();

        foreach (var exposedException in exposedExceptions)
        {
            shouldExposeExceptionTypeByException.Add(exposedException.GetType(), (exposedException, true));
        }

        foreach (var hiddenException in hiddenExceptions)
        {
            shouldExposeExceptionTypeByException.Add(hiddenException.GetType(), (hiddenException, false));
        }

        var currentDomainExceptions = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(type => typeof(Exception).IsAssignableFrom(type))
            .Select(type => CreateExceptionInstance(type) ?? CreateExceptionInstance(type, DefaultError))
            .WhereNotNull()
            .ToHashSet();

        foreach (var currentDomainException in currentDomainExceptions)
        {
            var currentDomainExceptionType = currentDomainException.GetType();

            shouldExposeExceptionTypeByException[currentDomainExceptionType] =
                shouldExposeExceptionTypeByException.GetValueOrDefault(currentDomainExceptionType) ?? (currentDomainException, false);
        }

        shouldExposeExceptionTypeByException
            .Select(kvp => ExceptionMapping.ExposeExceptionType(kvp.Value!.Value.Exception) == kvp.Value.Value.ShouldExposeExceptionType)
            .All(x => x)
            .Should()
            .BeTrue();
    }

    private Exception? CreateExceptionInstance(Type type, params object?[]? data)
    {
        try
        {
            return Activator.CreateInstance(type, data) as Exception;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
