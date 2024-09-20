// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Exceptions;

public class ContestLockedException : Exception
{
    public ContestLockedException()
        : base("Contest is past locked or archived and cannot be edited")
    {
    }
}
