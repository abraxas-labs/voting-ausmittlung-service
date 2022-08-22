// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using eCH_0222_1_0;
using Voting.Ausmittlung.Data.Models;
using ElectionCandidate = eCH_0222_1_0.ElectionCandidate;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class MajorityElectionRawDataMapping
{
    private static readonly ElectionBallotPosition IndividualPosition = new ElectionBallotPosition
    {
        Item = new ElectionCandidate
        {
            ItemsElementName = new[] { ItemsChoiceType.writeIn },
            Items = new[] { "Vereinzelte" },
        },
    };

    private static readonly ElectionBallotPosition EmptyPosition = new ElectionBallotPosition
    {
        Item = true,
    };

    internal static ElectionGroupBallotRawData ToEchElectionGroupRawData(this MajorityElectionResult electionResult)
    {
        return new ElectionGroupBallotRawData
        {
            ElectionGroupIdentification = electionResult.MajorityElection.ElectionGroup?.Id.ToString(),
            ElectionRawData = CreateElectionRawData(electionResult),
        };
    }

    private static ElectionRawDataType[] CreateElectionRawData(MajorityElectionResult electionResult)
    {
        return electionResult.SecondaryMajorityElectionResults
            .OrderBy(r => r.SecondaryMajorityElectionId)
            .Select(secondaryElectionResult => secondaryElectionResult.ToElectionRawData())
            .Append(electionResult.ToElectionRawData())
            .ToArray();
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
                .ToArray(),
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
                .ToArray(),
        };
    }

    private static ElectionBallotRawData ToEchElectionBallotRawData(this MajorityElectionResultBallot resultBallot)
    {
        var positions = resultBallot.BallotCandidates
            .Where(c => c.Selected)
            .Select(c => c.Candidate.ToEchElectionBallotPosition())
            .Concat(Enumerable.Repeat(EmptyPosition, resultBallot.EmptyVoteCount))
            .Concat(Enumerable.Repeat(IndividualPosition, resultBallot.IndividualVoteCount));

        return new ElectionBallotRawData
        {
            BallotPosition = positions.ToArray(),
        };
    }

    private static ElectionBallotRawData ToEchElectionBallotRawData(this SecondaryMajorityElectionResultBallot resultBallot)
    {
        var positions = resultBallot.BallotCandidates
            .Where(c => c.Selected)
            .Select(c => c.Candidate.ToEchElectionBallotPosition())
            .Concat(Enumerable.Repeat(EmptyPosition, resultBallot.EmptyVoteCount))
            .Concat(Enumerable.Repeat(IndividualPosition, resultBallot.IndividualVoteCount));

        return new ElectionBallotRawData
        {
            BallotPosition = positions.ToArray(),
        };
    }

    private static ElectionBallotRawData ToEchElectionBallotRawData(this MajorityElectionBallotGroupResult ballotGroupResult, Guid electionId)
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

        return new ElectionBallotRawData
        {
            BallotPosition = positions.ToArray(),
        };
    }

    private static ElectionBallotPosition ToEchElectionBallotPosition(this MajorityElectionCandidateBase candidate)
    {
        return new ElectionBallotPosition
        {
            Item = new ElectionCandidate
            {
                Items = new[] { candidate.Id.ToString(), candidate.Number },
                ItemsElementName = new[] { ItemsChoiceType.candidateIdentification, ItemsChoiceType.candidateReferenceOnPosition },
            },
        };
    }
}
