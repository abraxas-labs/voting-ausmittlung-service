// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Ech0222_1_0;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class ElectionRawDataMapping
{
    public static EVotingElectionResult ToEVotingElection(
        this IEnumerable<ElectionRawDataTypeBallotRawData> ballotRawData,
        string electionIdentification,
        string countingCircleBasisId)
    {
        var electionId = GuidParser.Parse(electionIdentification);
        var ballots = ballotRawData
            .Select(x => x.ToEVotingElectionBallot())
            .ToList();
        return new EVotingElectionResult(electionId, countingCircleBasisId, ballots);
    }

    private static EVotingElectionBallot ToEVotingElectionBallot(this ElectionRawDataTypeBallotRawData ballotRawData)
    {
        var listId = GuidParser.ParseNullable(ballotRawData.ListRawData?.ListIdentification);
        var unchanged = ballotRawData.IsUnchangedBallotValueSpecified && ballotRawData.IsUnchangedBallotValue;
        var positions =
            (IReadOnlyCollection<EVotingElectionBallotPosition>?)ballotRawData.BallotPosition?.Select(b => b.ToEVotingElectionBallotPosition()).ToList()
            ?? Array.Empty<EVotingElectionBallotPosition>();
        return new EVotingElectionBallot(listId, unchanged, positions);
    }

    private static EVotingElectionBallotPosition ToEVotingElectionBallotPosition(this ElectionRawDataTypeBallotRawDataBallotPosition position)
    {
        return position.Candidate != null ? BuildCandidate(position.Candidate) : EVotingElectionBallotPosition.Empty;
    }

    private static EVotingElectionBallotPosition BuildCandidate(ElectionRawDataTypeBallotRawDataBallotPositionCandidate candidate)
    {
        return !string.IsNullOrEmpty(candidate.WriteIn) ? EVotingElectionBallotPosition.ForWriteIn(candidate.WriteIn) : EVotingElectionBallotPosition.ForCandidateId(GuidParser.Parse(candidate.CandidateIdentification));
    }
}
