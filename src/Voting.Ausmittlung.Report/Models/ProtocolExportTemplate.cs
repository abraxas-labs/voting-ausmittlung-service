// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Models;

public class ProtocolExportTemplate
{
    public ProtocolExportTemplate(ResultExportTemplate template)
    {
        Template = template;
    }

    public ResultExportTemplate Template { get; }

    public ProtocolExport? ProtocolExport { get; set; }
}
