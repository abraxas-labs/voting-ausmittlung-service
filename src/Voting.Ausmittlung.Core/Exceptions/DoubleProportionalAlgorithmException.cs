// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Exceptions;

public class DoubleProportionalAlgorithmException : Exception
{
    public DoubleProportionalAlgorithmException(string msg)
        : base(msg)
    {
    }
}
