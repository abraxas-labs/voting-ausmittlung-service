// (c) Copyright by Abraxas Informatik AG
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

    public HashSet<string> ModificationUsers { get; } = [];

    public int CountOfBallots => BallotNumbers.Count;

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

    protected void EnsureCanCloseBundle()
    {
        if (BallotNumbers.Count == 0)
        {
            throw new ValidationException("at least one ballot is required to close this bundle");
        }
    }

    protected IEnumerable<int> GenerateBallotNumberSamples()
        => RandomUtil.Samples(BallotNumbers, BallotBundleSampleSize).OrderBy(x => x);

    protected void TrackPossibleModification(string userId)
    {
        if (State > BallotBundleState.InProcess)
        {
            ModificationUsers.Add(userId);
        }
    }

    protected void CheckBallotNumber(int? ballotNumber, bool automaticBallotNumberGeneration)
    {
        if (automaticBallotNumberGeneration && ballotNumber == null)
        {
            return;
        }

        if (automaticBallotNumberGeneration && ballotNumber != null)
        {
            throw new ValidationException("Automatic ballot number generation does not expect a ballot number to be set");
        }

        if (ballotNumber == null)
        {
            throw new ValidationException("Manual ballot number generation needs a ballot number to be set");
        }

        if (BallotNumbers.Contains(ballotNumber.Value))
        {
            throw new ValidationException("The bundle already contains this ballot number");
        }
    }
}
