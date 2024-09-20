// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public abstract class PdfBaseDetails
{
    public int TotalCountOfVoters { get; set; }

    public int TotalCountOfValidVotingCards { get; set; }

    public int TotalCountOfInvalidVotingCards { get; set; }

    public int TotalCountOfVotingCards { get; set; }

    public int TotalCountOfEVotingVotingCards { get; set; }

    [XmlElement("ContestVotingCardDetail")]
    public List<PdfVotingCardResultDetail>? VotingCards { get; set; }

    [XmlElement("CountOfVotersDetail")]
    public List<PdfCountOfVotersInformationSubTotal>? CountOfVotersInformationSubTotals { get; set; }
}
