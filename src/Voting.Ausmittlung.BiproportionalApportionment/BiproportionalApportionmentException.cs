// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.BiproportionalApportionment;

public class BiproportionalApportionmentException : Exception
{
    public BiproportionalApportionmentException(string message)
        : base(message)
    {
    }
}
