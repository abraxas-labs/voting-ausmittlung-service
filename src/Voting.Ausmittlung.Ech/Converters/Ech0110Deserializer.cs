// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
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

    public void DeserializeXml(Stream stream, Guid contestId, VotingImport importData)
    {
        var schemaSet = Ech0110Schemas.LoadEch0110Schemas();
        var delivery = _echDeserializer.DeserializeXml<Delivery>(stream, schemaSet);
        var deliveryContestId = GuidParser.Parse(delivery.ResultDelivery.ContestInformation.ContestIdentification);
        if (deliveryContestId != contestId)
        {
            throw new ValidationException("contestIds do not match");
        }

        importData.AddEchMessageId(delivery.DeliveryHeader.MessageId);
        importData.VotingCards = EVotingVotingCardImportFromDelivery(delivery);
        AddEVotingCountOfVotersInformationImportFromDelivery(delivery, importData);
    }

    private static List<VotingImportCountingCircleVotingCards> EVotingVotingCardImportFromDelivery(Delivery delivery)
    {
        var votingCards =
            new List<VotingImportCountingCircleVotingCards>(delivery.ResultDelivery.CountingCircleResults.Count);
        foreach (var ccData in delivery.ResultDelivery.CountingCircleResults)
        {
            var countingCircleId = ccData.CountingCircle.CountingCircleId;
            var receivedVotingCards = int.Parse(ccData.VotingCardsInformation.CountOfReceivedValidVotingCardsTotal);
            votingCards.Add(new VotingImportCountingCircleVotingCards(countingCircleId, receivedVotingCards));
        }

        return votingCards;
    }

    private static void AddEVotingCountOfVotersInformationImportFromDelivery(
        Delivery delivery,
        VotingImport importData)
    {
        foreach (var countingCircleResult in delivery.ResultDelivery.CountingCircleResults)
        {
            var countingCircleId = countingCircleResult.CountingCircle.CountingCircleId;

            if (countingCircleResult.VoteResultsSpecified)
            {
                foreach (var voteResult in countingCircleResult.VoteResults)
                {
                    if (!int.TryParse(
                            voteResult.CountOfVotersInformation.CountOfVotersTotal,
                            out var countOfVotersTotal))
                    {
                        continue;
                    }

                    importData.SetTotalCountOfVoters(
                        GuidParser.Parse(voteResult.Vote.VoteIdentification),
                        countingCircleId,
                        countOfVotersTotal);
                }
            }

            if (countingCircleResult.ElectionGroupResultsSpecified)
            {
                foreach (var electionGroupResult in countingCircleResult.ElectionGroupResults)
                {
                    if (!int.TryParse(
                            electionGroupResult.CountOfVotersInformation.CountOfVotersTotal,
                            out var countOfVotersTotal))
                    {
                        continue;
                    }

                    foreach (var result in electionGroupResult.ElectionResults)
                    {
                        importData.SetTotalCountOfVoters(
                            GuidParser.Parse(result.Election.ElectionIdentification),
                            countingCircleId,
                            countOfVotersTotal);
                    }
                }
            }
        }
    }
}
