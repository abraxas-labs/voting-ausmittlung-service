// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Controllers.Models;

public class GenerateResultBundleReviewExportRequest
{
    public string TemplateKey { get; set; } = string.Empty;

    public Guid ContestId { get; set; }

    public Guid CountingCircleId { get; set; }

    public Guid PoliticalBusinessResultBundleId { get; set; }

    public Guid PoliticalBusinessId { get; set; }
}
