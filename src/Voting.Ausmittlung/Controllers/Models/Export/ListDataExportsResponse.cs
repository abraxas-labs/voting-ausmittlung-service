﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Controllers.Models.Export;

public class ListDataExportsResponse
{
    public ContestResponse Contest { get; set; } = null!;

    public CountingCircleResponse? CountingCircle { get; set; }

    public IReadOnlyCollection<DataExportTemplate> Templates { get; set; } = null!;
}
