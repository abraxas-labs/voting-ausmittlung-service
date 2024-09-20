// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Ech0222_1_0;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class MajorityElectionRawDataMapping
{
    private static readonly ElectionRawDataTypeBallotRawDataBallotPosition IndividualPosition = new ElectionRawDataTypeBallotRawDataBallotPosition
    {
        Candidate = new ElectionRawDataTypeBallotRawDataBallotPositionCandidate { WriteIn = "Vereinzelte" },
        IsEmpty = false,
    };

    private static readonly ElectionRawDataTypeBallotRawDataBallotPosition EmptyPosition = new ElectionRawDataTypeBallotRawDataBallotPosition
    {
        IsEmpty = true,
    };

    internal static RawDataTypeCountingCircleRawDataElectionGroupBallotRawData ToEchElectionGroupRawData(this MajorityElectionResult electionResult)
    {
        return new RawDataTypeCountingCircleRawDataElectionGroupBallotRawData
        {
            ElectionGroupIdentification = electionResult.MajorityElection.ElectionGroup?.Id.ToString(),
            ElectionRawData = CreateElectionRawData(electionResult),
        };
    }

    private static List<ElectionRawDataType> CreateElectionRawData(MajorityElectionResult electionResult)
    {
        return electionResult.SecondaryMajorityElectionResults
            .OrderBy(r => r.SecondaryMajorityElectionId)
            .Select(secondaryElectionResult => secondaryElectionResult.ToElectionRawData())
            .Append(electionResult.ToElectionRawData())
            .ToList();
    }

    private static ElectionRawDataType ToElectionRawData(this MajorityElectionResult electionResult)
    {
        return new ElectionRawDataType
        {
            ElectionIdentification = electionResult.MajorityElectionId.ToString(),
            BallotRawData = electionResult
                .Bundles
                .Where(b => b.State == BallotBundleState.Reviewed)
                .SelectMany(b => b.Ballots)
                .OrderBy(x => x.Bundle.Number)
                .ThenBy(x => x.Number)
                .Select(b => b.ToEchElectionBallotRawData())
                .Concat(electionResult.BallotGroupResults
                    .Where(x => x.VoteCount > 0)
                    .SelectMany(g => Enumerable.Repeat(g.ToEchElectionBallotRawData(electionResult.MajorityElectionId), g.VoteCount)))
                .ToList(),
        };
    }

    private static ElectionRawDataType ToElectionRawData(this SecondaryMajorityElectionResult electionResult)
    {
        return new ElectionRawDataType
        {
            ElectionIdentification = electionResult.SecondaryMajorityElectionId.ToString(),
            BallotRawData = electionResult
                .ResultBallots
                .Select(b => b.ToEchElectionBallotRawData())
                .Concat(electionResult.PrimaryResult.BallotGroupResults
                    .SelectMany(g => Enumerable.Repeat(g.ToEchElectionBallotRawData(electionResult.SecondaryMajorityElectionId), g.VoteCount)))
                .ToList(),
        };
    }

    private static ElectionRawDataTypeBallotRawData ToEchElectionBallotRawData(this MajorityElectionResultBallot resultBallot)
    {
        var positions = resultBallot.BallotCandidates
            .Where(c => c.Selected)
            .Select(c => c.Candidate.ToEchElectionBallotPosition())
            .Concat(Enumerable.Repeat(EmptyPosition, resultBallot.EmptyVoteCount))
            .Concat(Enumerable.Repeat(IndividualPosition, resultBallot.IndividualVoteCount))
            .ToList();

        return new ElectionRawDataTypeBallotRawData
        {
            BallotPosition = positions,
        };
    }

    private static ElectionRawDataTypeBallotRawData ToEchElectionBallotRawData(this SecondaryMajorityElectionResultBallot resultBallot)
    {
        var positions = resultBallot.BallotCandidates
            .Where(c => c.Selected)
            .Select(c => c.Candidate.ToEchElectionBallotPosition())
            .Concat(Enumerable.Repeat(EmptyPosition, resultBallot.EmptyVoteCount))
            .Concat(Enumerable.Repeat(IndividualPosition, resultBallot.IndividualVoteCount))
            .ToList();

        return new ElectionRawDataTypeBallotRawData
        {
            BallotPosition = positions,
        };
    }

    private static ElectionRawDataTypeBallotRawData ToEchElectionBallotRawData(this MajorityElectionBallotGroupResult ballotGroupResult, Guid electionId)
    {
        var ballotGroupEntries =
            ballotGroupResult.BallotGroup.Entries
                .Where(entry => (entry.PrimaryMajorityElectionId ?? entry.SecondaryMajorityElectionId!) == electionId)
                .ToList();

        var positions = ballotGroupEntries.SelectMany(entry => entry.Candidates).Select(c =>
            c.PrimaryElectionCandidate != null
                ? ToEchElectionBallotPosition(c.PrimaryElectionCandidate)
                : ToEchElectionBallotPosition(c.SecondaryElectionCandidate!));
        positions = positions.Concat(ballotGroupEntries.SelectMany(entry => Enumerable.Repeat(EmptyPosition, entry.BlankRowCount)));
        positions = positions.Concat(ballotGroupEntries.SelectMany(entry => Enumerable.Repeat(IndividualPosition, entry.IndividualCandidatesVoteCount)));

        return new ElectionRawDataTypeBallotRawData
        {
            BallotPosition = positions.ToList(),
        };
    }

    private static ElectionRawDataTypeBallotRawDataBallotPosition ToEchElectionBallotPosition(this MajorityElectionCandidateBase candidate)
    {
        return new ElectionRawDataTypeBallotRawDataBallotPosition
        {
            Candidate = new ElectionRawDataTypeBallotRawDataBallotPositionCandidate
            {
                CandidateIdentification = candidate.Id.ToString(),
                CandidateReferenceOnPosition = candidate.Number,
            },
            IsEmpty = false,
        };
    }
}
