﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.EventSignature.Exceptions;

public class ContestMemoryCacheException : Exception
{
    public ContestMemoryCacheException(string? message)
        : base(message)
    {
    }
}
