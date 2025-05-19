// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;

namespace Voting.Ausmittlung.Core.Exceptions;

public class UnknownCountingCircleInImportException : ValidationException
{
    internal UnknownCountingCircleInImportException(Guid actualId, Guid expectedId)
        : base(
            $"Expected to import results for counting circle with id {expectedId}, but id in import is {actualId}.")
    {
    }
}
