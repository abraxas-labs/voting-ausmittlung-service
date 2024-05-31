// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Controllers.Models.Export;

public class ListProtocolExportStatesResponse
{
    public IReadOnlyCollection<ProtocolExportStateResponse> ProtocolExports { get; set; } = new List<ProtocolExportStateResponse>();
}
