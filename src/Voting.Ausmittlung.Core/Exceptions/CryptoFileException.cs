// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Exceptions;

public class CryptoFileException : Exception
{
    public CryptoFileException(string? message)
        : base(message)
    {
    }
}
