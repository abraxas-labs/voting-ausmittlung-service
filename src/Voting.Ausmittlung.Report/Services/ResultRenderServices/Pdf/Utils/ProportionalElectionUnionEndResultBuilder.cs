// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public class ProportionalElectionUnionEndResultBuilder
{
    public ReportProportionalElectionUnionEndResult BuildEndResult(ProportionalElectionUnion union)
    {
        var endResult = new ReportProportionalElectionUnionEndResult();
        endResult.TotalCountOfVoters = union.ProportionalElectionUnionEntries
                .Sum(x => x.ProportionalElection.EndResult!.TotalCountOfVoters);

        endResult.TotalCountOfBlankRowsOnListsWithoutParty = union.ProportionalElectionUnionEntries
            .Sum(x => x.ProportionalElection.EndResult!.TotalCountOfBlankRowsOnListsWithoutParty);

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
        // group by short description to combine lists with the same short description but different order numbers
        return unionLists
            .GroupBy(x => x.ShortDescription)
            .Select(g =>
        {
            var listEndResults = g.SelectMany(y => y.ProportionalElectionUnionListEntries)
                .Select(x => x.ProportionalElectionList.EndResult!)
                .ToList();

            // since the short description is the same, we can use the first list in the group by and just update the order number
            var unionList = g.First();
            unionList.OrderNumber = string.Join(", ", g.OrderBy(x => x.OrderNumber).Select(x => x.OrderNumber));

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
