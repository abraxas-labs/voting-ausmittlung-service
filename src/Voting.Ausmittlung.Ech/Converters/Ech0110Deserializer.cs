// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ech0110_4_0;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Common;
using Voting.Lib.Ech;
using Voting.Lib.Ech.Ech0110_4_0.Schemas;

namespace Voting.Ausmittlung.Ech.Converters;

public class Ech0110Deserializer
{
    private readonly EchDeserializer _echDeserializer;

    public Ech0110Deserializer(EchDeserializer echDeserializer)
    {
        _echDeserializer = echDeserializer;
    }

    public (EVotingVotingCardImport EVotingVotingCardImport, EVotingCountOfVotersInformationImport EVotingCountOfVotersInformationImport) DeserializeXml(Stream stream)
    {
        var schemaSet = Ech0110Schemas.LoadEch0110Schemas();
        var delivery = _echDeserializer.DeserializeXml<Delivery>(stream, schemaSet);
        return (EVotingVotingCardImportFromDelivery(delivery), EVotingCountOfVotersInformationImportFromDelivery(delivery));
    }

    private static EVotingVotingCardImport EVotingVotingCardImportFromDelivery(Delivery delivery)
    {
        var contestId = GuidParser.Parse(delivery.ResultDelivery.ContestInformation.ContestIdentification);

        var votingCards = new List<EVotingCountingCircleVotingCards>();
        foreach (var ccData in delivery.ResultDelivery.CountingCircleResults)
        {
            var countingCircleId = ccData.CountingCircle.CountingCircleId;
            var receivedVotingCards = int.Parse(ccData.VotingCardsInformation.CountOfReceivedValidVotingCardsTotal);
            votingCards.Add(new EVotingCountingCircleVotingCards(countingCircleId, receivedVotingCards));
        }

        return new EVotingVotingCardImport(delivery.DeliveryHeader.MessageId, contestId, votingCards);
    }

    private static EVotingCountOfVotersInformationImport EVotingCountOfVotersInformationImportFromDelivery(Delivery delivery)
    {
        var contestId = GuidParser.Parse(delivery.ResultDelivery.ContestInformation.ContestIdentification);
        var countingCircleResultsCountOfVotersInformations = new List<EVotingCountingCircleResultCountOfVotersInformation>();

        foreach (var countingCircleResult in delivery.ResultDelivery.CountingCircleResults)
        {
            var countingCircleId = countingCircleResult.CountingCircle.CountingCircleId;

            if (countingCircleResult.VoteResultsSpecified)
            {
                foreach (var voteResult in countingCircleResult.VoteResults)
                {
                    if (!int.TryParse(voteResult.CountOfVotersInformation.CountOfVotersTotal, out var countOfVotersTotal))
                    {
                        continue;
                    }

                    countingCircleResultsCountOfVotersInformations.Add(new EVotingCountingCircleResultCountOfVotersInformation(
                        countingCircleId,
                        GuidParser.Parse(voteResult.Vote.VoteIdentification),
                        countOfVotersTotal));
                }
            }

            if (countingCircleResult.ElectionGroupResultsSpecified)
            {
                foreach (var electionGroupResult in countingCircleResult.ElectionGroupResults)
                {
                    if (!int.TryParse(electionGroupResult.CountOfVotersInformation.CountOfVotersTotal, out var countOfVotersTotal))
                    {
                        continue;
                    }

                    countingCircleResultsCountOfVotersInformations.AddRange(electionGroupResult.ElectionResults
                        .Where(er => er.ProportionalElection != null)
                        .Select(er => new EVotingCountingCircleResultCountOfVotersInformation(
                            countingCircleId,
                            GuidParser.Parse(er.Election.ElectionIdentification),
                            countOfVotersTotal)));

                    countingCircleResultsCountOfVotersInformations.AddRange(electionGroupResult.ElectionResults
                        .Where(er => er.MajoralElection != null)
                        .Select(er => new EVotingCountingCircleResultCountOfVotersInformation(
                            countingCircleId,
                            GuidParser.Parse(er.Election.ElectionIdentification),
                            countOfVotersTotal)));
                }
            }
        }

        return new EVotingCountOfVotersInformationImport(
            contestId,
            countingCircleResultsCountOfVotersInformations);
    }
}
