// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;

namespace Voting.Ausmittlung.Core.Exceptions;

public class CountingCircleResultInInvalidStateForEVotingImportException : ValidationException
{
    internal CountingCircleResultInInvalidStateForEVotingImportException(Guid id)
        : base($"A result is in an invalid state for an eVoting import to be possible ({id})")
    {
    }
}
