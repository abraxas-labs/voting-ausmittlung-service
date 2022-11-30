// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public abstract class CountingCircleResultAggregate : BaseEventSignatureAggregate
{
    public abstract Guid PoliticalBusinessId { get; }

    public Guid CountingCircleId { get; protected set; }

    public CountingCircleResultState State { get; protected set; } = CountingCircleResultState.Initial;

    public abstract void StartSubmission(Guid countingCircleId, Guid politicalBusinessId, Guid contestId, bool testingPhaseEnded);

    public abstract void SubmissionFinished(Guid contestId);

    public abstract void CorrectionFinished(string comment, Guid contestId);

    public abstract void ResetToSubmissionFinished(Guid contestId);

    public abstract void AuditedTentatively(Guid contestId);

    public abstract void Plausibilise(Guid contestId);

    public abstract void ResetToAuditedTentatively(Guid contestId);

    public abstract void FlagForCorrection(Guid contestId, string comment = "");

    public abstract void Reset(Guid contestId);

    protected void EnsureInState(params CountingCircleResultState[] validStates)
    {
        if (!validStates.Contains(State))
        {
            throw new ValidationException($"This operation is not possible for state {State}");
        }
    }

    protected void EnsureInTestingPhase()
    {
        if (Id != AusmittlungUuidV5.BuildPoliticalBusinessResult(PoliticalBusinessId, CountingCircleId, false))
        {
            throw new ValidationException($"Counting circle result {Id} is not in testing phase");
        }
    }
}
