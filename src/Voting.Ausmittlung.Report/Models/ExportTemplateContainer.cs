// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Models;

public class ExportTemplateContainer<T>
{
    public ExportTemplateContainer(
        Contest contest,
        CountingCircle? countingCircle,
        IReadOnlyCollection<T> templates)
    {
        Contest = contest;
        CountingCircle = countingCircle;
        Templates = templates;
    }

    public Contest Contest { get; }

    public CountingCircle? CountingCircle { get; }

    public IReadOnlyCollection<T> Templates { get; }
}
