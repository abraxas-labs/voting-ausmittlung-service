// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public interface IPoliticalBusinessEndResultAggregate
{
    ActionId PrepareFinalize(Guid politicalBusinessId, bool testingPhaseEnded);

    void Finalize(Guid politicalBusinessId, Guid contestId, bool testingPhaseEnded);

    void RevertFinalization(Guid contestId);
}
