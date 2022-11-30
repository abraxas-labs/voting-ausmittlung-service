// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Exceptions;

public class ContestTestingPhaseEndedException : Exception
{
    public ContestTestingPhaseEndedException()
    : base("Contest testing phase has ended")
    {
    }
}
