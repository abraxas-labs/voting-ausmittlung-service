// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public static class PdfProportionalElectionListResultVoteSourceBuilder
{
    private static readonly IEqualityComparer<PdfProportionalElectionSimpleList> ListComparer =
        new PropertyEqualityComparer<PdfProportionalElectionSimpleList, Guid>(x => x.Id);

    public static void BuildVoteSourceSums(IEnumerable<PdfProportionalElectionListEndResult> results)
    {
        foreach (var result in results)
        {
            result.VoteSources = result.CandidateEndResults
                ?.Where(x => x.VoteSources != null)
                .SelectMany(x => x.VoteSources!)
                .GroupBy(x => x.List!, x => x, ListComparer)
                .Select(x => new PdfProportionalElectionListVoteSourceResult
                {
                    List = x.Key,
                    VoteCount = x.Sum(y => y.VoteCount),
                })
                .OrderBy(x => x.List == null)
                .ThenBy(x => x.List?.Position)
                .ToList();

            foreach (var voteSource in result.VoteSources!)
            {
                if (result.List != null && voteSource.List?.Id == result.List.Id)
                {
                    voteSource.TotalVoteCount = result.BlankRowsCount + voteSource.VoteCount;
                }
                else
                {
                    voteSource.TotalVoteCount = voteSource.VoteCount;
                }
            }
        }
    }

    public static void BuildVoteSourceSums(IEnumerable<PdfProportionalElectionListResult> results)
    {
        foreach (var result in results)
        {
            result.VoteSources = result.CandidateResults
                ?.Where(x => x.VoteSources != null)
                .SelectMany(x => x.VoteSources!)
                .GroupBy(x => x.List!, x => x, ListComparer)
                .Select(x => new PdfProportionalElectionListVoteSourceResult
                {
                    List = x.Key,
                    VoteCount = x.Sum(y => y.VoteCount),
                })
                .OrderBy(x => x.List == null)
                .ThenBy(x => x.List?.Position)
                .ToList();

            foreach (var voteSource in result.VoteSources!)
            {
                if (result.List != null && voteSource.List?.Id == result.List.Id)
                {
                    voteSource.TotalVoteCount = result.BlankRowsCount + voteSource.VoteCount;
                }
                else
                {
                    voteSource.TotalVoteCount = voteSource.VoteCount;
                }
            }
        }
    }
}
