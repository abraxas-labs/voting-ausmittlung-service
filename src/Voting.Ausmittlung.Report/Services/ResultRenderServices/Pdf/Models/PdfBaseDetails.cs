// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public abstract class PdfBaseDetails
{
    [XmlIgnore]
    public int TotalCountOfVoters { get; set; }

    [XmlElement("TotalCountOfVoters")]
    public string TotalCountOfVotersFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCountOfValidVotingCards { get; set; }

    [XmlElement("TotalCountOfValidVotingCards")]
    public string TotalCountOfValidVotingCardsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCountOfInvalidVotingCards { get; set; }

    [XmlElement("TotalCountOfInvalidVotingCards")]
    public string TotalCountOfInvalidVotingCardsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCountOfVotingCards { get; set; }

    [XmlElement("TotalCountOfVotingCards")]
    public string TotalCountOfVotingCardsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCountOfEVotingVotingCards { get; set; }

    [XmlElement("TotalCountOfEVotingVotingCards")]
    public string TotalCountOfEVotingVotingCardsFormatted { get; set; } = string.Empty;

    [XmlElement("ContestVotingCardDetail")]
    public List<PdfVotingCardResultDetail>? VotingCards { get; set; }

    [XmlElement("CountOfVotersDetail")]
    public List<PdfCountOfVotersInformationSubTotal>? CountOfVotersInformationSubTotals { get; set; }

    public void FormatNumbers(bool useLegacyFormat)
    {
        TotalCountOfVotersFormatted = NumberFormatUtil.FormatInteger(TotalCountOfVoters, useLegacyFormat);
        TotalCountOfVotingCardsFormatted = NumberFormatUtil.FormatInteger(TotalCountOfVotingCards, useLegacyFormat);
        TotalCountOfInvalidVotingCardsFormatted = NumberFormatUtil.FormatInteger(TotalCountOfInvalidVotingCards, useLegacyFormat);
        TotalCountOfEVotingVotingCardsFormatted = NumberFormatUtil.FormatInteger(TotalCountOfEVotingVotingCards, useLegacyFormat);
        TotalCountOfValidVotingCardsFormatted = NumberFormatUtil.FormatInteger(TotalCountOfValidVotingCards, useLegacyFormat);

        foreach (var votingCard in VotingCards ?? [])
        {
            votingCard.FormatNumbers(useLegacyFormat);
        }

        foreach (var subTotal in CountOfVotersInformationSubTotals ?? [])
        {
            subTotal.FormatNumbers(useLegacyFormat);
        }
    }
}
