// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Models;

public class ResultExportTemplateContainer
{
    public ResultExportTemplateContainer(
        Contest contest,
        CountingCircle? countingCircle,
        IReadOnlyCollection<ResultExportTemplate> templates)
    {
        Contest = contest;
        CountingCircle = countingCircle;
        Templates = templates;
    }

    public Contest Contest { get; }

    public CountingCircle? CountingCircle { get; }

    public IReadOnlyCollection<ResultExportTemplate> Templates { get; }
}