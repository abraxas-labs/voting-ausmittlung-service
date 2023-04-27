// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Messaging.Messages;

// The name of the rabbit queue is generated based on the FQN of this class.
// If it is moved or renamed this should be considered.
public class ProtocolExportStateChanged
{
    public ProtocolExportStateChanged(Guid protocolExportId, Guid exportTemplateId, ProtocolExportState newState, string fileName, DateTime started)
    {
        ProtocolExportId = protocolExportId;
        ExportTemplateId = exportTemplateId;
        NewState = newState;
        FileName = fileName;
        Started = started;
    }

    public Guid ProtocolExportId { get; }

    public Guid ExportTemplateId { get; }

    public ProtocolExportState NewState { get; }

    public string FileName { get; }

    public DateTime Started { get; }
}
