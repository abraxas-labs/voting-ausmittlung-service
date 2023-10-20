// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Report.EventLogs;

namespace Voting.Ausmittlung.Test.Mocks;

public class StaticEventLogBuilder : EventLogBuilder
{
    public StaticEventLogBuilder(
        EventLogInitializerAdapterRegistry eventLogInitializerRegistry,
        ILogger<EventLogBuilder> logger,
        EventLogEventSignatureVerifier eventLogEventSignatureVerifier)
        : base(eventLogInitializerRegistry, logger, eventLogEventSignatureVerifier)
    {
    }

    public override EventLog BuildSignatureEventLog(IMessage message)
    {
        // These events contain dynamic data which changes with each run, making it impossible to compare via snapshots
        // Since this data is important, we only remove it in tests
        switch (message)
        {
            case EventSignaturePublicKeyCreated createdEvent:
                createdEvent.AuthenticationTag = ByteString.Empty;
                break;
            case EventSignaturePublicKeyDeleted deletedEvent:
                deletedEvent.AuthenticationTag = ByteString.Empty;
                break;
            case Abraxas.Voting.Basis.Events.V1.EventSignaturePublicKeyCreated createdEvent:
                createdEvent.AuthenticationTag = ByteString.Empty;
                break;
            case Abraxas.Voting.Basis.Events.V1.EventSignaturePublicKeyDeleted deletedEvent:
                deletedEvent.AuthenticationTag = ByteString.Empty;
                break;
        }

        return base.BuildSignatureEventLog(message);
    }
}
