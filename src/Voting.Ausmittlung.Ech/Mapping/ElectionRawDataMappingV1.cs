// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Ech0222_1_0;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class ElectionRawDataMappingV1
{
    public static VotingImportElectionResult ToEVotingElection(
        this IEnumerable<ElectionRawDataTypeBallotRawData> ballotRawData,
        string electionIdentification,
        string countingCircleBasisId)
    {
        var electionId = GuidParser.Parse(electionIdentification);
        var ballots = ballotRawData
            .Select(x => x.ToEVotingElectionBallot())
            .ToList();
        return new VotingImportElectionResult(electionId, countingCircleBasisId, ballots);
    }

    private static VotingElectionBallot ToEVotingElectionBallot(this ElectionRawDataTypeBallotRawData ballotRawData)
    {
        var listId = GuidParser.ParseNullable(ballotRawData.ListRawData?.ListIdentification);
        var unchanged = ballotRawData.IsUnchangedBallotValueSpecified && ballotRawData.IsUnchangedBallotValue;
        var positions =
            (IReadOnlyCollection<VotingImportElectionBallotPosition>?)ballotRawData.BallotPosition?.Select(b => b.ToEVotingElectionBallotPosition()).ToList()
            ?? Array.Empty<VotingImportElectionBallotPosition>();
        return new VotingElectionBallot(listId, unchanged, positions);
    }

    private static VotingImportElectionBallotPosition ToEVotingElectionBallotPosition(this ElectionRawDataTypeBallotRawDataBallotPosition position)
    {
        return position.Candidate != null ? BuildCandidate(position.Candidate) : VotingImportElectionBallotPosition.Empty;
    }

    private static VotingImportElectionBallotPosition BuildCandidate(ElectionRawDataTypeBallotRawDataBallotPositionCandidate candidate)
    {
        return !string.IsNullOrEmpty(candidate.WriteIn) ? VotingImportElectionBallotPosition.ForWriteIn(candidate.WriteIn) : VotingImportElectionBallotPosition.ForCandidateId(GuidParser.Parse(candidate.CandidateIdentification));
    }
}
