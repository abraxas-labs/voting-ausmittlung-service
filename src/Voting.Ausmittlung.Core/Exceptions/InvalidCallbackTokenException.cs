// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.ComponentModel.DataAnnotations;

namespace Voting.Ausmittlung.Core.Exceptions;

public class InvalidCallbackTokenException : ValidationException
{
    internal InvalidCallbackTokenException()
        : base("Callback token does not match")
    {
    }
}
