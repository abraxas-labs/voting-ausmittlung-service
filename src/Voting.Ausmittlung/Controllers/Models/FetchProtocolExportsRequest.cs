// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Controllers.Models;

public class FetchProtocolExportsRequest
{
    public Guid ContestId { get; set; }

    public Guid? CountingCircleId { get; set; }

    public List<Guid> ProtocolExportIds { get; set; } = new();
}
