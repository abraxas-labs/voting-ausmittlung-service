// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfPoliticalBusinessCountOfVoters
{
    private decimal _voterParticipation;

    [XmlIgnore]
    public decimal VoterParticipation
    {
        get
        {
            return _voterParticipation;
        }

        set
        {
            _voterParticipation = decimal.Round(value, 4, MidpointRounding.AwayFromZero);
        }
    }

    [XmlElement("VoterParticipation")]
    public string VoterParticipationFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalReceivedBallots { get; set; }

    [XmlElement("TotalReceivedBallots")]
    public string TotalReceivedBallotsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalUnaccountedBallots { get; set; }

    [XmlElement("TotalUnaccountedBallots")]
    public string TotalUnaccountedBallotsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int EVotingReceivedBallots { get; set; }

    [XmlElement("EVotingReceivedBallots")]
    public string EVotingReceivedBallotsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int EVotingInvalidBallots { get; set; }

    [XmlElement("EVotingInvalidBallots")]
    public string EVotingInvalidBallotsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int EVotingBlankBallots { get; set; }

    [XmlElement("EVotingBlankBallots")]
    public string EVotingBlankBallotsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int EVotingAccountedBallots { get; set; }

    [XmlElement("EVotingAccountedBallots")]
    public string EVotingAccountedBallotsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int EVotingTotalUnaccountedBallots { get; set; }

    [XmlElement("EVotingTotalUnaccountedBallots")]
    public string EVotingTotalUnaccountedBallotsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int ConventionalReceivedBallots { get; set; }

    [XmlElement("ConventionalReceivedBallots")]
    public string ConventionalReceivedBallotsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int ConventionalInvalidBallots { get; set; }

    [XmlElement("ConventionalInvalidBallots")]
    public string ConventionalInvalidBallotsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int ConventionalBlankBallots { get; set; }

    [XmlElement("ConventionalBlankBallots")]
    public string ConventionalBlankBallotsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int ConventionalAccountedBallots { get; set; }

    [XmlElement("ConventionalAccountedBallots")]
    public string ConventionalAccountedBallotsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int ConventionalTotalUnaccountedBallots { get; set; }

    [XmlElement("ConventionalTotalUnaccountedBallots")]
    public string ConventionalTotalUnaccountedBallotsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalAccountedBallots { get; set; }

    [XmlElement("CountOfAccountedBallots")] // legacy name
    public string TotalAccountedBallotsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalInvalidBallots { get; set; }

    [XmlElement("CountOfInvalidBallots")] // legacy name
    public string TotalInvalidBallotsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalBlankBallots { get; set; }

    [XmlElement("CountOfBlankBallots")] // legacy name
    public string TotalBlankBallotsFormatted { get; set; } = string.Empty;

    public void FormatNumbers(bool useLegacyFormat)
    {
        VoterParticipationFormatted = NumberFormatUtil.FormatPercentage(VoterParticipation, useLegacyFormat);
        TotalReceivedBallotsFormatted = NumberFormatUtil.FormatInteger(TotalReceivedBallots, useLegacyFormat);
        TotalUnaccountedBallotsFormatted = NumberFormatUtil.FormatInteger(TotalUnaccountedBallots, useLegacyFormat);
        EVotingReceivedBallotsFormatted = NumberFormatUtil.FormatInteger(EVotingReceivedBallots, useLegacyFormat);
        EVotingInvalidBallotsFormatted = NumberFormatUtil.FormatInteger(EVotingInvalidBallots, useLegacyFormat);
        EVotingBlankBallotsFormatted = NumberFormatUtil.FormatInteger(EVotingBlankBallots, useLegacyFormat);
        EVotingAccountedBallotsFormatted = NumberFormatUtil.FormatInteger(EVotingAccountedBallots, useLegacyFormat);
        EVotingTotalUnaccountedBallotsFormatted = NumberFormatUtil.FormatInteger(EVotingTotalUnaccountedBallots, useLegacyFormat);
        ConventionalReceivedBallotsFormatted = NumberFormatUtil.FormatInteger(ConventionalReceivedBallots, useLegacyFormat);
        ConventionalInvalidBallotsFormatted = NumberFormatUtil.FormatInteger(ConventionalInvalidBallots, useLegacyFormat);
        ConventionalBlankBallotsFormatted = NumberFormatUtil.FormatInteger(ConventionalBlankBallots, useLegacyFormat);
        ConventionalAccountedBallotsFormatted = NumberFormatUtil.FormatInteger(ConventionalAccountedBallots, useLegacyFormat);
        ConventionalTotalUnaccountedBallotsFormatted = NumberFormatUtil.FormatInteger(ConventionalTotalUnaccountedBallots, useLegacyFormat);
        TotalAccountedBallotsFormatted = NumberFormatUtil.FormatInteger(TotalAccountedBallots, useLegacyFormat);
        TotalInvalidBallotsFormatted = NumberFormatUtil.FormatInteger(TotalInvalidBallots, useLegacyFormat);
        TotalBlankBallotsFormatted = NumberFormatUtil.FormatInteger(TotalBlankBallots, useLegacyFormat);
    }
}
