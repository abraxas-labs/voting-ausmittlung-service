// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfCountOfVotersInformationSubTotal
{
    public SexType Sex { get; set; }

    [XmlIgnore]
    public int CountOfVoters { get; set; }

    [XmlElement("CountOfVoters")]
    public string CountOfVotersFormatted { get; set; } = string.Empty;

    public VoterType VoterType { get; set; }

    public DomainOfInfluenceType DomainOfInfluenceType { get; set; }

    public void FormatNumbers(bool useLegacyFormat)
    {
        CountOfVotersFormatted = NumberFormatUtil.FormatInteger(CountOfVoters, useLegacyFormat);
    }
}
