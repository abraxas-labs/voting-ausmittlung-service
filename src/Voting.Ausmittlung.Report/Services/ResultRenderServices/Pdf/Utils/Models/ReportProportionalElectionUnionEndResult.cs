﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

public class ReportProportionalElectionUnionEndResult
{
    public PoliticalBusinessCountOfVoters CountOfVoters { get; set; }
        = new PoliticalBusinessCountOfVoters();

    public int NumberOfMandates { get; set; }

    public int ListVotesCount { get; set; }

    public int BlankRowsCount { get; set; }

    public int TotalVoteCount => ListVotesCount + BlankRowsCount;

    public int TotalCountOfVoters { get; set; }

    public int TotalCountOfBlankRowsOnListsWithoutParty { get; set; }

    public int TotalVoteCountInclWithoutParty => TotalVoteCount + TotalCountOfBlankRowsOnListsWithoutParty;

    public ICollection<ProportionalElectionUnionListEndResult> UnionListEndResults { get; set; }
        = new HashSet<ProportionalElectionUnionListEndResult>();
}
