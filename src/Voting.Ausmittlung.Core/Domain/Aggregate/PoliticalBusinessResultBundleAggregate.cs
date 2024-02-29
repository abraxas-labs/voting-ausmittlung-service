// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public abstract class PoliticalBusinessResultBundleAggregate : BaseEventSignatureAggregate
{
    public int CurrentBallotNumber { get; protected set; }

    public List<int> BallotNumbers { get; } = new();

    public Guid PoliticalBusinessResultId { get; protected set; }

    public string CreatedBy { get; protected set; } = string.Empty;

    public BallotBundleState State { get; protected set; } = BallotBundleState.InProcess;

    public int BundleNumber { get; protected set; }

    protected abstract int BallotBundleSampleSize { get; }

    protected void EnsureHasBallot(int ballotNumber)
    {
        if (!BallotNumbers.Contains(ballotNumber))
        {
            throw new ValidationException("ballot number not found");
        }
    }

    protected void EnsureInState(params BallotBundleState[] validStates)
    {
        if (!validStates.Contains(State))
        {
            throw new ValidationException($"This operation is not possible for state {State}");
        }
    }

    protected IEnumerable<int> GenerateBallotNumberSamples()
        => RandomUtil.Samples(BallotNumbers, BallotBundleSampleSize).OrderBy(x => x);
}
