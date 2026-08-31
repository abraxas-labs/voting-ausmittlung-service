// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfVotingCardResultDetail
{
    [XmlIgnore]
    public int CountOfReceivedVotingCards { get; set; }

    [XmlElement("CountOfReceivedVotingCards")]
    public string CountOfReceivedVotingCardsFormatted { get; set; } = string.Empty;

    public bool Valid { get; set; }

    public PdfVotingChannel Channel { get; set; }

    public DomainOfInfluenceType DomainOfInfluenceType { get; set; }

    public void FormatNumbers(bool useLegacyFormat)
    {
        CountOfReceivedVotingCardsFormatted = NumberFormatUtil.FormatInteger(CountOfReceivedVotingCards, useLegacyFormat);
    }
}
