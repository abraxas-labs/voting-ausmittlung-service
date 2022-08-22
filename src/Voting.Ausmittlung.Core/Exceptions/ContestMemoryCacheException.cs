// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Exceptions;

public class ContestMemoryCacheException : Exception
{
    public ContestMemoryCacheException(string? message)
        : base(message)
    {
    }
}
