// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Utils;

public static class MajorityElectionResultUtils
{
    public static void RemoveCountToIndividualCandidatesAndAdjustTotals(MajorityElection election)
    {
        if (election.EndResult != null)
        {
            RemoveCountToIndividualCandidatesAndAdjustTotals(election.EndResult);
        }

        foreach (var result in election.Results ?? new List<MajorityElectionResult>())
        {
            RemoveCountToIndividualCandidatesAndAdjustTotals(result);
        }

        if (election.MajorityElectionCandidates != null)
        {
            election.MajorityElectionCandidates = election.MajorityElectionCandidates
                .Where(c => c.ReportingType is not MajorityElectionCandidateReportingType.CountToIndividual)
                .ToList() ?? new List<MajorityElectionCandidate>();
        }

        foreach (var secondaryElection in election.SecondaryMajorityElections ?? new List<SecondaryMajorityElection>())
        {
            if (secondaryElection.Candidates != null)
            {
                secondaryElection.Candidates = secondaryElection.Candidates
                    .Where(c => c.ReportingType is not MajorityElectionCandidateReportingType.CountToIndividual)
                    .ToList();
            }
        }
    }

    public static void RemoveCountToIndividualCandidatesAndAdjustTotals(MajorityElectionResult result)
    {
        foreach (var secondaryResult in result.SecondaryMajorityElectionResults ?? new List<SecondaryMajorityElectionResult>())
        {
            RemoveCountToIndividualCandidatesAndAdjustTotals(secondaryResult);
        }

        var countToIndividualCandidateResults = result.CandidateResults
            .Where(c => c.Candidate.CountToIndividual)
            .ToList();

        result.CandidateResults = result.CandidateResults
            .Where(c => !c.Candidate.CountToIndividual)
            .ToList();

        foreach (var candidateResult in countToIndividualCandidateResults)
        {
            result.ForEachSubTotal(
                (st, dataSource) =>
                {
                    var voteCount = candidateResult.GetVoteCountOfDataSource(dataSource);
                    st.IndividualVoteCount += voteCount;
                    st.TotalCandidateVoteCountExclIndividual -= voteCount;
                },
                (st, dataSource) =>
                {
                    st.IndividualVoteCount ??= 0;
                    var voteCount = candidateResult.GetVoteCountOfDataSource(dataSource);
                    st.IndividualVoteCount += voteCount;
                    st.TotalCandidateVoteCountExclIndividual -= voteCount;
                });
        }
    }

    public static void RemoveCountToIndividualCandidatesAndAdjustTotals(SecondaryMajorityElectionResult result)
    {
        var countToIndividualCandidateResults = result.CandidateResults
            .Where(c => c.Candidate.CountToIndividual)
            .ToList();

        result.CandidateResults = result.CandidateResults
            .Where(c => !c.Candidate.CountToIndividual)
            .ToList();

        foreach (var candidateResult in countToIndividualCandidateResults)
        {
            result.ForEachSubTotal(
                (st, dataSource) =>
                {
                    var voteCount = candidateResult.GetVoteCountOfDataSource(dataSource);
                    st.IndividualVoteCount += voteCount;
                    st.TotalCandidateVoteCountExclIndividual -= voteCount;
                },
                (st, dataSource) =>
                {
                    st.IndividualVoteCount ??= 0;
                    var voteCount = candidateResult.GetVoteCountOfDataSource(dataSource);
                    st.IndividualVoteCount += voteCount;
                    st.TotalCandidateVoteCountExclIndividual -= voteCount;
                });
        }
    }

    public static void RemoveCountToIndividualCandidatesAndAdjustTotals(MajorityElectionEndResult endResult)
    {
        foreach (var secondaryEndResult in endResult.SecondaryMajorityElectionEndResults ?? new List<SecondaryMajorityElectionEndResult>())
        {
            RemoveCountToIndividualCandidatesAndAdjustTotals(secondaryEndResult);
        }

        var countToIndividualCandidateEndResults = endResult.CandidateEndResults
            .Where(c => c.Candidate.CountToIndividual)
            .ToList();

        endResult.CandidateEndResults = endResult.CandidateEndResults
            .Where(c => !c.Candidate.CountToIndividual)
            .ToList();

        foreach (var candidateEndResult in countToIndividualCandidateEndResults)
        {
            endResult.ForEachSubTotal((st, dataSource) =>
            {
                var voteCount = candidateEndResult.GetVoteCountOfDataSource(dataSource);
                st.IndividualVoteCount += voteCount;
                st.TotalCandidateVoteCountExclIndividual -= voteCount;
            });
        }
    }

    public static void RemoveCountToIndividualCandidatesAndAdjustTotals(SecondaryMajorityElectionEndResult endResult)
    {
        var countToIndividualCandidateEndResults = endResult.CandidateEndResults
            .Where(c => c.Candidate.CountToIndividual)
            .ToList();

        endResult.CandidateEndResults = endResult.CandidateEndResults
            .Where(c => !c.Candidate.CountToIndividual)
            .ToList();

        foreach (var candidateEndResult in countToIndividualCandidateEndResults)
        {
            endResult.ForEachSubTotal((st, dataSource) =>
            {
                var voteCount = candidateEndResult.GetVoteCountOfDataSource(dataSource);
                st.IndividualVoteCount += voteCount;
                st.TotalCandidateVoteCountExclIndividual -= voteCount;
            });
        }
    }
}
