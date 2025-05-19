// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfPoliticalBusinessCountOfVoters
{
    private decimal _voterParticipation;

    public decimal VoterParticipation
    {
        get
        {
            return _voterParticipation;
        }

        set
        {
            _voterParticipation = decimal.Round(value, 4);
        }
    }

    public int TotalReceivedBallots { get; set; }

    public int TotalUnaccountedBallots { get; set; }

    public int EVotingReceivedBallots { get; set; }

    public int EVotingInvalidBallots { get; set; }

    public int EVotingBlankBallots { get; set; }

    public int EVotingAccountedBallots { get; set; }

    public int EVotingTotalUnaccountedBallots { get; set; }

    public int ConventionalReceivedBallots { get; set; }

    public int ConventionalInvalidBallots { get; set; }

    public int ConventionalBlankBallots { get; set; }

    public int ConventionalAccountedBallots { get; set; }

    public int ConventionalTotalUnaccountedBallots { get; set; }

    [XmlElement("CountOfAccountedBallots")] // legacy name
    public int TotalAccountedBallots { get; set; }

    [XmlElement("CountOfInvalidBallots")] // legacy name
    public int TotalInvalidBallots { get; set; }

    [XmlElement("CountOfBlankBallots")] // legacy name
    public int TotalBlankBallots { get; set; }
}
