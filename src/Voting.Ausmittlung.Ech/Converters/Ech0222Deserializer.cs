// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Voting.Ausmittlung.Ech.Mapping;
using Voting.Ausmittlung.Ech.Models;
using Voting.Ausmittlung.Ech.Schemas;
using Voting.Lib.Common;
using Delivery = eCH_0222_1_0.Standard.Delivery;

namespace Voting.Ausmittlung.Ech.Converters;

public static class Ech0222Deserializer
{
    public static EVotingImport DeserializeXml(Stream stream)
    {
        var schemaSet = Ech0222SchemaLoader.LoadEch0222Schemas();
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

        return EVotingImportFromDelivery(delivery);
    }

    private static EVotingImport EVotingImportFromDelivery(Delivery delivery)
    {
        if (delivery.RawDataDelivery?.RawData?.ContestIdentification == null)
        {
            throw new ValidationException("Could not deserialize eCH");
        }

        var contestId = GuidParser.Parse(delivery.RawDataDelivery.RawData.ContestIdentification);

        if (delivery.RawDataDelivery.RawData.CountingCircleRawData == null)
        {
            return new EVotingImport(delivery.DeliveryHeader.MessageId, contestId, Array.Empty<EVotingPoliticalBusinessResult>());
        }

        var results = new List<EVotingPoliticalBusinessResult>();
        foreach (var ccData in delivery.RawDataDelivery.RawData.CountingCircleRawData)
        {
            var countingCircleBasisId = GuidParser.Parse(ccData.countingCircleId);

            if (ccData.VoteRawData != null)
            {
                var ccVotes = ccData.VoteRawData
                    .Select(x => x.ToEVotingVote(countingCircleBasisId));
                results.AddRange(ccVotes);
            }

            if (ccData.electionGroupBallotRawData != null)
            {
                var ccElections = ccData.electionGroupBallotRawData
                    .SelectMany(g => g.ElectionRawData)
                    .Select(x => x.ToEVotingElection(countingCircleBasisId))
                    .Where(x => x != null)
                    .Select(x => x!);
                results.AddRange(ccElections);
            }
        }

        return new EVotingImport(
            delivery.DeliveryHeader.MessageId,
            contestId,
            results);
    }
}
