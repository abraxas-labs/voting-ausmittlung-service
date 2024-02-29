// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public class ProportionalElectionUnionEndResultBuilder
{
    public ProportionalElectionUnionEndResult BuildEndResult(ProportionalElectionUnion union)
    {
        var endResult = new ProportionalElectionUnionEndResult();
        endResult.TotalCountOfVoters = union.ProportionalElectionUnionEntries
                .Sum(x => x.ProportionalElection.EndResult!.TotalCountOfVoters);

        var countOfVoters = union.ProportionalElectionUnionEntries
            .Select(x => x.ProportionalElection.EndResult!.CountOfVoters)
            .ToList();
        endResult.CountOfVoters = PoliticalBusinessCountOfVotersUtils.SumCountOfVoters(countOfVoters, endResult.TotalCountOfVoters);

        endResult.UnionListEndResults = BuildUnionListEndResults(union.ProportionalElectionUnionLists.ToList());

        foreach (var unionListEndResult in endResult.UnionListEndResults)
        {
            endResult.NumberOfMandates += unionListEndResult.NumberOfMandates;
            endResult.ListVotesCount += unionListEndResult.ListVotesCount;
            endResult.BlankRowsCount += unionListEndResult.BlankRowsCount;
        }

        return endResult;
    }

    private List<ProportionalElectionUnionListEndResult> BuildUnionListEndResults(IEnumerable<ProportionalElectionUnionList> unionLists)
    {
        return unionLists.Select(unionList =>
        {
            var listEndResults = unionList
                .ProportionalElectionUnionListEntries
                .Select(x => x.ProportionalElectionList.EndResult!)
                .ToList();

            var unionListEndResult = new ProportionalElectionUnionListEndResult
            {
                UnionList = unionList,
            };

            foreach (var listEndResult in listEndResults)
            {
                unionListEndResult.NumberOfMandates += listEndResult.NumberOfMandates;
                unionListEndResult.BlankRowsCount += listEndResult.BlankRowsCount;
                unionListEndResult.ListVotesCount += listEndResult.ListVotesCount;
            }

            return unionListEndResult;
        }).OrderBy(x => x.UnionList!.OrderNumber)
          .ThenBy(x => x.UnionList!.ShortDescription)
          .ToList();
    }
}
