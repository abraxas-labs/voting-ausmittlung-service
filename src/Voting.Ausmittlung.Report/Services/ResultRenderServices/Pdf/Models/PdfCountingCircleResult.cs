// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public abstract class PdfCountingCircleResult
{
    public PdfCountingCircle? CountingCircle { get; set; }

    [XmlIgnore]
    public int TotalCountOfVoters { get; set; }

    [XmlElement("TotalCountOfVoters")]
    public string TotalCountOfVotersFormatted { get; set; } = string.Empty;

    public CountingCircleResultState State { get; set; }

    public bool IsAuditedTentativelyOrPlausibilised { get; set; }

    public virtual void FormatNumbers(bool useLegacyFormat)
    {
        CountingCircle?.FormatNumbers(useLegacyFormat);
        TotalCountOfVotersFormatted = NumberFormatUtil.FormatInteger(TotalCountOfVoters, useLegacyFormat);
    }
}
