// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionUnionEndResult
{
    public PdfPoliticalBusinessCountOfVoters? CountOfVoters { get; set; }

    public int NumberOfMandates { get; set; }

    public int ListVotesCount { get; set; }

    public int BlankRowsCount { get; set; }

    public int TotalVoteCount { get; set; }

    public int TotalCountOfBlankRowsOnListsWithoutParty { get; set; }

    public int TotalVoteCountInclWithoutParty { get; set; }

    [XmlElement("ProportionalElectionUnionListEndResult")]
    public List<PdfProportionalElectionUnionListEndResult>? UnionListEndResults { get; set; }
}
