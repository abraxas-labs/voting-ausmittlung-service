// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Ech0222_1_0;
using Voting.Ausmittlung.Ech.Mapping;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Common;
using Voting.Lib.Ech;
using Voting.Lib.Ech.Ech0222_1_0.Schemas;

namespace Voting.Ausmittlung.Ech.Converters;

public class Ech0222_1_0_Deserializer : IEch0222Deserializer
{
    private readonly EchDeserializer _echDeserializer;

    public Ech0222_1_0_Deserializer(EchDeserializer echDeserializer)
    {
        _echDeserializer = echDeserializer;
    }

    public VotingImport DeserializeXml(Stream stream)
    {
        var schemaSet = Ech0222Schemas.LoadEch0222Schemas();
        var delivery = _echDeserializer.DeserializeXml<Delivery>(stream, schemaSet);
        return EVotingImportFromDelivery(delivery);
    }

    private static VotingImport EVotingImportFromDelivery(Delivery delivery)
    {
        if (delivery.RawDataDelivery?.RawData?.ContestIdentification == null)
        {
            throw new ValidationException("Could not deserialize eCH");
        }

        var contestId = GuidParser.Parse(delivery.RawDataDelivery.RawData.ContestIdentification);
        var import = new VotingImport(delivery.DeliveryHeader.MessageId, contestId);
        if (delivery.RawDataDelivery.RawData.CountingCircleRawData == null)
        {
            return import;
        }

        foreach (var ccData in delivery.RawDataDelivery.RawData.CountingCircleRawData)
        {
            var ccVotes = ccData.VoteRawData
                .Select(x => x.ToEVotingVote(ccData.CountingCircleId));
            import.AddResults(ccVotes);

            var ccElections = ccData.ElectionGroupBallotRawData
                .SelectMany(g => g.ElectionRawData)
                .GroupBy(x => x.ElectionIdentification)
                .Select(x => x.SelectMany(g => g.BallotRawData).ToEVotingElection(x.Key, ccData.CountingCircleId))
                .Select(x => x);
            import.AddResults(ccElections);

            if (ccData.VoteRawData.Count == 0 && ccData.ElectionGroupBallotRawData.Count == 0)
            {
                import.AddEmptyResult(ccData.CountingCircleId);
            }
        }

        return import;
    }
}
