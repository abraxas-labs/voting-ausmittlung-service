// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public static class PdfMajorityElectionEndResultUtil
{
    public static void MapCandidateEndResultsToStateLists(PdfMajorityElectionEndResult endResult)
    {
        var candidateEndResultsByState = endResult
            .CandidateEndResults?
            .GroupBy(x => x.State)
            .ToDictionary(x => x.Key, x => x.ToList())
            ?? throw new ValidationException("Candidate end results must not be null.");

        foreach (var state in candidateEndResultsByState.Keys)
        {
            var candidateEndResults = candidateEndResultsByState[state]
                .OrderByDescending(x => x.VoteCount)
                .ThenBy(x => x.Rank)
                .ThenBy(x => x.Candidate!.Position)
                .ToList();

            switch (state)
            {
                case MajorityElectionCandidateEndResultState.Pending:
                    endResult.CandidateEndResultsPending = candidateEndResults;
                    break;
                case MajorityElectionCandidateEndResultState.AbsoluteMajorityAndElected:
                    endResult.CandidateEndResultsAbsoluteMajorityAndElected = candidateEndResults;
                    break;
                case MajorityElectionCandidateEndResultState.AbsoluteMajorityAndNotElected:
                    endResult.CandidateEndResultsAbsoluteMajorityAndNotElected = candidateEndResults;
                    break;
                case MajorityElectionCandidateEndResultState.NoAbsoluteMajorityAndNotElectedButRankOk:
                    endResult.CandidateEndResultsNoAbsoluteMajorityAndNotElectedButRankOk = candidateEndResults;
                    break;
                case MajorityElectionCandidateEndResultState.Elected:
                    endResult.CandidateEndResultsElected = candidateEndResults;
                    break;
                case MajorityElectionCandidateEndResultState.NotElected:
                    endResult.CandidateEndResultsNotElected = candidateEndResults;
                    break;
                default:
                    throw new InvalidOperationException($"bad candidate end result state: {state}");
            }
        }

        endResult.CandidateEndResults = null;
    }
}
