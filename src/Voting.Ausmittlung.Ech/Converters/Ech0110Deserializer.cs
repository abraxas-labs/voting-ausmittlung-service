// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Xml.Serialization;
using eCH_0110_4_0;
using Voting.Ausmittlung.Ech.Models;
using Voting.Ausmittlung.Ech.Schemas;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Ech.Converters;

public static class Ech0110Deserializer
{
    public static EVotingVotingCardImport DeserializeXml(Stream stream)
    {
        var schemaSet = Ech0110SchemaLoader.LoadEch0110Schemas();
        using var reader = XmlUtil.CreateReaderWithSchemaValidation(stream, schemaSet);
        var serializer = new XmlSerializer(typeof(Delivery));

        Delivery delivery;
        try
        {
            delivery = (Delivery?)serializer.Deserialize(reader)
                ?? throw new ValidationException("Deserialization with returned null");
        }
        catch (InvalidOperationException ex) when (ex.InnerException != null)
        {
            // The XmlSerializer wraps all exceptions into an InvalidOperationException.
            // Unwrap it to surface the "correct" exception type.
            throw ex.InnerException;
        }

        return EVotingVotingCardImportFromDelivery(delivery);
    }

    private static EVotingVotingCardImport EVotingVotingCardImportFromDelivery(Delivery delivery)
    {
        var contestId = GuidParser.Parse(delivery.ResultDelivery.ContestInformation.ContestIdentification);

        var votingCards = new List<EVotingCountingCircleVotingCards>();
        foreach (var ccData in delivery.ResultDelivery.CountingCircleResults)
        {
            var countingCircleId = ccData.CountingCircle.CountingCircleId;
            var receivedVotingCards = ccData.VotingCardsInformation.CountOfReceivedValidVotingCardsTotal;
            votingCards.Add(new EVotingCountingCircleVotingCards(countingCircleId, receivedVotingCards));
        }

        return new EVotingVotingCardImport(delivery.DeliveryHeader.MessageId, contestId, votingCards);
    }
}
