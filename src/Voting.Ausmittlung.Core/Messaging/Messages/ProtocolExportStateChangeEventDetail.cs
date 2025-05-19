// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Messaging.Messages;

public record ProtocolExportStateChangeEventDetail(Guid ExportTemplateId, Guid ProtocolExportId, string FileName, ProtocolExportState NewState);
