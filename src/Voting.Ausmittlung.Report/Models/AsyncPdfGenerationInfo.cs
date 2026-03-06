// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Models;

public class AsyncPdfGenerationInfo
{
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the priority of the async job. The lower the value, the higher the priority.
    /// If null, the default priority of the PDF service is used.
    /// </summary>
    public int? AsyncJobPriority { get; set; }
}
