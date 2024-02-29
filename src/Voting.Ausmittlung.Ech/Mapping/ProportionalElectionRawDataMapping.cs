// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Ech0222_1_0;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class ProportionalElectionRawDataMapping
{
    private static readonly ElectionRawDataTypeBallotRawDataBallotPosition EmptyPosition = new() { IsEmpty = true };

    internal static RawDataTypeCountingCircleRawDataElectionGroupBallotRawData ToEchElectionGroupRawData(this ProportionalElectionResult electionResult)
    {
        return new RawDataTypeCountingCircleRawDataElectionGroupBallotRawData
        {
            // ElectionGroupIdentification currently only supported for MajorityElection
            ElectionRawData = CreateElectionRawData(electionResult),
        };
    }

    private static List<ElectionRawDataType> CreateElectionRawData(ProportionalElectionResult electionResult)
    {
        var rawData = new ElectionRawDataType
        {
            ElectionIdentification = electionResult.ProportionalElectionId.ToString(),
            BallotRawData = electionResult.Bundles
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
                .ToList(),
        };

        return new List<ElectionRawDataType> { rawData };
    }

    private static ElectionRawDataTypeBallotRawData ToEchElectionBallotRawData(this ProportionalElectionResultBallot resultBallot)
    {
        return new ElectionRawDataTypeBallotRawData
        {
            IsUnchangedBallot = false,
            ListRawData =
                resultBallot.Bundle.ListId != null ? new ElectionRawDataTypeBallotRawDataListRawData { ListIdentification = resultBallot.Bundle.ListId.ToString() } : null,
            BallotPosition = CreateElectionBallotPositions(resultBallot),
        };
    }

    private static List<ElectionRawDataTypeBallotRawDataBallotPosition> CreateElectionBallotPositions(ProportionalElectionResultBallot resultBallot)
    {
        var emptyPositions = Enumerable.Repeat(EmptyPosition, resultBallot.EmptyVoteCount);
        return resultBallot.BallotCandidates
            .Where(c => !c.RemovedFromList)
            .OrderBy(x => x.Position)
            .Select(c => c.Candidate.ToEchElectionBallotPosition())
            .Concat(emptyPositions)
            .ToList();
    }

    private static IEnumerable<ElectionRawDataTypeBallotRawData> ToEchElectionBallotRawData(
        this IEnumerable<ProportionalElectionUnmodifiedListResult> unmodifiedListResults)
    {
        return unmodifiedListResults.SelectMany(result => Enumerable.Repeat(
            new ElectionRawDataTypeBallotRawData
            {
                IsUnchangedBallot = true,
                ListRawData = new ElectionRawDataTypeBallotRawDataListRawData { ListIdentification = result.ListId.ToString() },
                BallotPosition = CreateElectionBallotPositions(result),
            },
            result.VoteCount));
    }

    private static List<ElectionRawDataTypeBallotRawDataBallotPosition> CreateElectionBallotPositions(ProportionalElectionUnmodifiedListResult unmodifiedListResult)
    {
        var emptyRows = Enumerable.Repeat(EmptyPosition, unmodifiedListResult.List.BlankRowCount);
        return ExpandCandidates(unmodifiedListResult.List.ProportionalElectionCandidates)
            .OrderBy(x => x.Position)
            .Select(x => x.Candidate.ToEchElectionBallotPosition())
            .Concat(emptyRows)
            .ToList();
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

    private static ElectionRawDataTypeBallotRawDataBallotPosition ToEchElectionBallotPosition(this ProportionalElectionCandidate candidate)
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
