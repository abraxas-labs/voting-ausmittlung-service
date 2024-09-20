// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates;

public class EventSignaturePublicKeyAggregateData
{
    public EventSignaturePublicKeyAggregateData(EventSignaturePublicKeyAggregateCreateData createData)
    {
        CreateData = createData;
    }

    public string KeyId => CreateData.KeyId;

    public EventSignaturePublicKeyAggregateCreateData CreateData { get; }

    public EventSignaturePublicKeyAggregateDeleteData? DeleteData { get; set; }
}
