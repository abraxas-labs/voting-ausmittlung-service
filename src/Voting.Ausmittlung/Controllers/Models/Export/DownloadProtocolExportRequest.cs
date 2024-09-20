// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Controllers.Models.Export;

public class DownloadProtocolExportRequest
{
    public Guid ContestId { get; set; }

    public Guid? CountingCircleId { get; set; }

    public Guid ProtocolExportId { get; set; }
}
