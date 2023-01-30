// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Globalization;
using System.Linq;
using eCH_0110_4_0;
using eCH_0155_4_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class MajorityElectionResultMapping
{
    internal static ElectionGroupResultsType ToEchElectionGroupResult(this MajorityElectionResult electionResult)
    {
        var electionResults = electionResult.SecondaryMajorityElectionResults
            .Select(secondaryResult => secondaryResult.ToEchElectionResult())
            .Append(electionResult.ToEchElectionResult())
            .OrderBy(r => r.Election.ElectionPosition)
            .ToArray();

        return new ElectionGroupResultsType
        {
            DomainOfInfluenceIdentification = electionResult.MajorityElection.DomainOfInfluence.BasisDomainOfInfluenceId.ToString(),
            ElectionGroupIdentification = electionResult.MajorityElection.ElectionGroup?.Id.ToString(),
            ElectionResults = electionResults,
            CountOfAccountedBallots = ResultDetailFromTotal(electionResult.CountOfVoters.TotalAccountedBallots),
            CountOfReceivedBallotsTotal = ResultDetailFromTotal(electionResult.CountOfVoters.TotalReceivedBallots),
            CountOfUnaccountedBallots = ResultDetailFromTotal(electionResult.CountOfVoters.TotalUnaccountedBallots),
            CountOfUnaccountedBlankBallots = ResultDetailFromTotal(electionResult.CountOfVoters.ConventionalBlankBallots.GetValueOrDefault()),
            CountOfUnaccountedInvalidBallots = ResultDetailFromTotal(electionResult.CountOfVoters.TotalInvalidBallots),
            CountOfVotersInformation = new CountOfVotersInformationType { CountOfVotersTotal = electionResult.TotalCountOfVoters.ToString(CultureInfo.InvariantCulture) },
        };
    }

    private static ElectionResultType ToEchElectionResult(this MajorityElectionResult electionResult)
    {
        var election = electionResult.MajorityElection;

        return new ElectionResultType
        {
            Election = ElectionType.Create(election.Id.ToString(), TypeOfElectionType.Majorz, election.NumberOfMandates),
            Item = new MajoralElection
            {
                CountOfBlankVotesTotal = ResultDetailFromTotal(electionResult.EmptyVoteCount),
                CountOfIndividualVotesTotal = ResultDetailFromTotal(electionResult.IndividualVoteCount),
                CountOfInvalidVotesTotal = ResultDetailFromTotal(electionResult.InvalidVoteCount),
                Candidate = electionResult.CandidateResults.OrderBy(r => r.Candidate.Position).Select(c => c.ToEchCandidateResult(c.Candidate)).ToArray(),
            },
        };
    }

    private static ElectionResultType ToEchElectionResult(this SecondaryMajorityElectionResult electionResult)
    {
        var election = electionResult.SecondaryMajorityElection;

        return new ElectionResultType
        {
            Election = ElectionType.Create(election.Id.ToString(), TypeOfElectionType.Majorz, election.NumberOfMandates),
            Item = new MajoralElection
            {
                CountOfBlankVotesTotal = ResultDetailFromTotal(electionResult.EmptyVoteCount),
                CountOfIndividualVotesTotal = ResultDetailFromTotal(electionResult.IndividualVoteCount),
                CountOfInvalidVotesTotal = ResultDetailFromTotal(electionResult.InvalidVoteCount),
                Candidate = electionResult.CandidateResults.OrderBy(x => x.Candidate.Position).Select(c => c.ToEchCandidateResult(c.Candidate)).ToArray(),
            },
        };
    }

    private static CandidateResultType ToEchCandidateResult(this MajorityElectionCandidateResultBase candidateResult, MajorityElectionCandidateBase candidate)
    {
        var candidateText = $"{candidate.PoliticalLastName} {candidate.PoliticalFirstName}";
        var texts = Languages.All
            .Select(l => CandidateTextInfo.Create(l, candidateText))
            .ToList();

        return new CandidateResultType
        {
            CountOfVotesTotal = candidateResult.VoteCount.ToString(CultureInfo.InvariantCulture),
            Item = new CandidateInformationType
            {
                OfficialCandidateYesNo = true,
                CandidateIdentification = candidate.Id.ToString(),
                FamilyName = candidate.LastName,
                FirstName = candidate.FirstName,
                CallName = candidate.PoliticalFirstName,
                CandidateReference = candidate.Number,
                CandidateText = CandidateTextInformation.Create(texts),
            },
        };
    }

    private static ResultDetailType ResultDetailFromTotal(int total)
    {
        return new ResultDetailType
        {
            Total = total.ToString(CultureInfo.InvariantCulture),
        };
    }
}
