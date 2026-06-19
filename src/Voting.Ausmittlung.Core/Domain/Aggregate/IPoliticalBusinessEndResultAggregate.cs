// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public interface IPoliticalBusinessEndResultAggregate
{
    IActionId PrepareFinalize(Guid politicalBusinessId, bool testingPhaseEnded);

    void Finalize(Guid politicalBusinessId, Guid contestId, bool testingPhaseEnded);

    void RevertFinalization(Guid contestId);
}
