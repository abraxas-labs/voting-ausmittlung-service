// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public abstract class PdfPoliticalBusinessEndResult
{
    [XmlIgnore]
    public int TotalCountOfVoters { get; set; }

    [XmlElement("TotalCountOfVoters")]
    public string TotalCountOfVotersFormatted { get; set; } = string.Empty;

    public int CountOfDoneCountingCircles { get; set; }

    public int TotalCountOfCountingCircles { get; set; }

    public bool AllCountingCirclesDone { get; set; }

    public virtual void FormatNumbers(bool useLegacyFormat)
    {
        TotalCountOfVotersFormatted = NumberFormatUtil.FormatInteger(TotalCountOfVoters, useLegacyFormat);
    }
}
