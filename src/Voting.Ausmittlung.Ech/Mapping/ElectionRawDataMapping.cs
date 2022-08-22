// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using eCH_0222_1_0;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class ElectionRawDataMapping
{
    public static EVotingElectionResult? ToEVotingElection(this ElectionRawDataType? electionRawData, Guid countingCircleBasisId)
    {
        if (electionRawData?.BallotRawData == null)
        {
            return null;
        }

        var electionId = GuidParser.Parse(electionRawData.ElectionIdentification);
        var ballots = electionRawData.BallotRawData.Select(x => x.ToEVotingElectionBallot()).ToList();
        return new EVotingElectionResult(electionId, countingCircleBasisId, ballots);
    }

    private static EVotingElectionBallot ToEVotingElectionBallot(this ElectionBallotRawData ballotRawData)
    {
        var listId = GuidParser.ParseNullable(ballotRawData.ListRawData?.ListIdentification);
        var unchanged = ballotRawData.IsUnchangedBallotSpecified && ballotRawData.IsUnchangedBallot;
        var positions =
            (IReadOnlyCollection<EVotingElectionBallotPosition>?)ballotRawData.BallotPosition?.Select(b => b.ToEVotingElectionBallotPosition()).ToList()
            ?? Array.Empty<EVotingElectionBallotPosition>();
        return new EVotingElectionBallot(listId, unchanged, positions);
    }

    private static EVotingElectionBallotPosition ToEVotingElectionBallotPosition(this ElectionBallotPosition position)
    {
        return position.Item switch
        {
            bool b when b => EVotingElectionBallotPosition.Empty,
            ElectionCandidate c => BuildCandidate(c),
            _ => throw new ValidationException("could not extract candidate from ballot position"),
        };
    }

    private static EVotingElectionBallotPosition BuildCandidate(ElectionCandidate candidate)
    {
        for (var i = 0; i < candidate.ItemsElementName.Length; i++)
        {
            var elName = candidate.ItemsElementName[i];
            var item = candidate.Items[i];
            switch (elName)
            {
                case ItemsChoiceType.writeIn:
                    return EVotingElectionBallotPosition.ForWriteIn(item);
                case ItemsChoiceType.candidateIdentification:
                    return EVotingElectionBallotPosition.ForCandidateId(GuidParser.Parse(item));
            }
        }

        throw new ValidationException($"could not extract candidate id for candidate [{string.Join(", ", candidate.Items)}; {string.Join(", ", candidate.ItemsElementName)}]");
    }
}
