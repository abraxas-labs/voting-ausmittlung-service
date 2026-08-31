// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfHagenbachBischoffSimpleGroup
{
    public string ShortDescription { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string AllListNumbers { get; set; } = string.Empty;

    [XmlIgnore]
    public int VoteCount { get; set; }

    [XmlElement("VoteCount")]
    public string VoteCountFormatted { get; set; } = string.Empty;

    public virtual void FormatNumbers(bool useLegacyFormat)
    {
        VoteCountFormatted = NumberFormatUtil.FormatInteger(VoteCount, useLegacyFormat);
    }
}
