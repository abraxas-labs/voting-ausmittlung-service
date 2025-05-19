// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;

namespace Voting.Ausmittlung.Core.Exceptions;

public class ECountingImportDisabledException : ValidationException
{
    internal ECountingImportDisabledException(Guid countingCircleId)
        : base($"The counting circle with id {countingCircleId} was not found or does not have eCounting enabled.")
    {
    }
}
