// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Core.Messaging.Messages;

public record EventProcessedMessageDetails(
    PoliticalBusinessResultBundleLogMessageDetail? BundleLog = null,
    ResultImportCountingCircleCompletedMessageDetail? CountingCircleImportCompleted = null,
    WriteInsMappedMessageDetail? WriteInsMapped = null,
    ProtocolExportStateChangeEventDetail? ProtocolExportStateChange = null);
