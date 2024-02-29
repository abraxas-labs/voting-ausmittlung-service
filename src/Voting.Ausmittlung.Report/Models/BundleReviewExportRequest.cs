// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.VotingExports.Models;

namespace Voting.Ausmittlung.Report.Models;

public class BundleReviewExportRequest
{
    public Guid PoliticalBusinessId { get; set; }

    public Guid CountingCircleId { get; set; }

    public Guid PoliticalBusinessResultBundleId { get; set; }

    public TemplateModel Template { get; set; } = null!;
}
