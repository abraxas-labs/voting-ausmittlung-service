// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using eCH_0222_1_0;
using Voting.Ausmittlung.Data.Models;
using ElectionCandidate = eCH_0222_1_0.ElectionCandidate;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class ProportionalElectionRawDataMapping
{
    private static readonly ElectionBallotPosition EmptyPosition = new ElectionBallotPosition { Item = true };

    internal static ElectionGroupBallotRawData ToEchElectionGroupRawData(this ProportionalElectionResult electionResult)
    {
        return new ElectionGroupBallotRawData
        {
            // ElectionGroupIdentification currently only supported for MajorityElection
            ElectionRawData = CreateElectionRawData(electionResult),
        };
    }

    private static ElectionRawDataType[] CreateElectionRawData(ProportionalElectionResult electionResult)
    {
        var rawData = new ElectionRawDataType
        {
            ElectionIdentification = electionResult.ProportionalElectionId.ToString(),
            BallotRawData = electionResult
                .Bundles
                .Where(b => b.State == BallotBundleState.Reviewed)
                .SelectMany(b => b.Ballots)
                .OrderBy(x => x.Bundle.Number)
                .ThenBy(x => x.Number)
                .Select(ToEchElectionBallotRawData)
                .Concat(electionResult
                    .UnmodifiedListResults
                    .Where(x => x.VoteCount > 0)
                    .OrderBy(x => x.List.Position)
                    .ToEchElectionBallotRawData())
                .ToArray(),
        };

        return new[] { rawData };
    }

    private static ElectionBallotRawData ToEchElectionBallotRawData(this ProportionalElectionResultBallot resultBallot)
    {
        return new ElectionBallotRawData
        {
            IsUnchangedBallot = false,
            IsUnchangedBallotSpecified = true,
            ListRawData =
                resultBallot.Bundle.ListId != null ? new ElectionListRawData { ListIdentification = resultBallot.Bundle.ListId.ToString() } : null,
            BallotPosition = CreateElectionBallotPositions(resultBallot),
        };
    }

    private static ElectionBallotPosition[] CreateElectionBallotPositions(ProportionalElectionResultBallot resultBallot)
    {
        var emptyPositions = Enumerable.Repeat(EmptyPosition, resultBallot.EmptyVoteCount);
        return resultBallot.BallotCandidates
            .Where(c => !c.RemovedFromList)
            .OrderBy(x => x.Position)
            .Select(c => c.Candidate.ToEchElectionBallotPosition())
            .Concat(emptyPositions)
            .ToArray();
    }

    private static IEnumerable<ElectionBallotRawData> ToEchElectionBallotRawData(
        this IEnumerable<ProportionalElectionUnmodifiedListResult> unmodifiedListResults)
    {
        return unmodifiedListResults.SelectMany(result => Enumerable.Repeat(
            new ElectionBallotRawData
            {
                IsUnchangedBallot = true,
                IsUnchangedBallotSpecified = true,
                ListRawData = new ElectionListRawData { ListIdentification = result.ListId.ToString() },
                BallotPosition = CreateElectionBallotPositions(result),
            },
            result.VoteCount));
    }

    private static ElectionBallotPosition[] CreateElectionBallotPositions(ProportionalElectionUnmodifiedListResult unmodifiedListResult)
    {
        var emptyRows = Enumerable.Repeat(EmptyPosition, unmodifiedListResult.List.BlankRowCount);
        return ExpandCandidates(unmodifiedListResult.List.ProportionalElectionCandidates)
            .OrderBy(x => x.Position)
            .Select(x => x.Candidate.ToEchElectionBallotPosition())
            .Concat(emptyRows)
            .ToArray();
    }

    private static IEnumerable<(int Position, ProportionalElectionCandidate Candidate)> ExpandCandidates(IEnumerable<ProportionalElectionCandidate> candidates)
    {
        foreach (var candidate in candidates)
        {
            yield return (candidate.Position, candidate);

            if (candidate.Accumulated)
            {
                yield return (candidate.AccumulatedPosition, candidate);
            }
        }
    }

    private static ElectionBallotPosition ToEchElectionBallotPosition(this ProportionalElectionCandidate candidate)
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
