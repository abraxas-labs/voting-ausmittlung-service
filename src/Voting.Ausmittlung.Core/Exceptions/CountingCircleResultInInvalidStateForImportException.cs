// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;

namespace Voting.Ausmittlung.Core.Exceptions;

public class CountingCircleResultInInvalidStateForImportException : ValidationException
{
    internal CountingCircleResultInInvalidStateForImportException(Guid id)
        : base($"A result is in an invalid state for an import to be possible ({id})")
    {
    }
}
