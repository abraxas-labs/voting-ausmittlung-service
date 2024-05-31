﻿// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Controllers.Models.Export;

public class DownloadDataExportRequest
{
    public Guid ContestId { get; set; }

    public Guid? CountingCircleId { get; set; }

    public Guid ExportTemplateId { get; set; }
}
