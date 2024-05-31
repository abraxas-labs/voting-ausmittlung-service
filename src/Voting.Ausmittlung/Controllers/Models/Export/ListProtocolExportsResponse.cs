// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Controllers.Models.Export;

public class ListProtocolExportsResponse
{
    public ContestResponse Contest { get; set; } = null!;

    public CountingCircleResponse? CountingCircle { get; set; }

    public IReadOnlyCollection<ProtocolExportResponse> ProtocolExports { get; set; } = null!;
}
