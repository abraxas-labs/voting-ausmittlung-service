// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Globalization;
using System.Linq;
using Ech0110_4_0;
using Ech0155_4_0;
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
            .ToList();

        return new ElectionGroupResultsType
        {
            DomainOfInfluenceIdentification = electionResult.MajorityElection.DomainOfInfluence.BasisDomainOfInfluenceId.ToString(),
            ElectionGroupIdentification = electionResult.MajorityElection.ElectionGroup?.Id.ToString(),
            ElectionResults = electionResults,
            CountOfAccountedBallots = ResultDetailFromTotal(electionResult.CountOfVoters.TotalAccountedBallots),
            CountOfReceivedBallotsTotal = ResultDetailFromTotal(electionResult.CountOfVoters.TotalReceivedBallots),
            CountOfUnaccountedBallots = ResultDetailFromTotal(electionResult.CountOfVoters.TotalUnaccountedBallots),
            CountOfUnaccountedBlankBallots = ResultDetailFromTotal(electionResult.CountOfVoters.TotalBlankBallots),
            CountOfUnaccountedInvalidBallots = ResultDetailFromTotal(electionResult.CountOfVoters.TotalInvalidBallots),
            CountOfVotersInformation = new CountOfVotersInformationType { CountOfVotersTotal = electionResult.TotalCountOfVoters.ToString(CultureInfo.InvariantCulture) },
        };
    }

    private static ElectionResultType ToEchElectionResult(this MajorityElectionResult electionResult)
    {
        var election = electionResult.MajorityElection;
        var candidates = electionResult.CandidateResults
            .OrderBy(r => r.Candidate.Position)
            .Select(c => c.ToEchCandidateResult(c.Candidate))
            .ToList();

        return new ElectionResultType
        {
            Election = new ElectionType
            {
                ElectionIdentification = election.Id.ToString(),
                TypeOfElection = TypeOfElectionType.Item2,
                NumberOfMandates = election.NumberOfMandates.ToString(),
            },
            MajoralElection = new ElectionResultTypeMajoralElection
            {
                CountOfBlankVotesTotal = ResultDetailFromTotal(electionResult.EmptyVoteCount),
                CountOfInvalidVotesTotal = ResultDetailFromTotal(electionResult.InvalidVoteCount),
                Candidate = candidates,
                CountOfIndividualVotesTotal = !election.IndividualCandidatesDisabled
                    ? ResultDetailFromTotal(electionResult.IndividualVoteCount)
                    : null,
            },
        };
    }

    private static ElectionResultType ToEchElectionResult(this SecondaryMajorityElectionResult electionResult)
    {
        var election = electionResult.SecondaryMajorityElection;
        var candidates = electionResult.CandidateResults
            .OrderBy(x => x.Candidate.Position)
            .Select(c => c.ToEchCandidateResult(c.Candidate))
            .ToList();

        return new ElectionResultType
        {
            Election = new ElectionType
            {
                ElectionIdentification = election.Id.ToString(),
                TypeOfElection = TypeOfElectionType.Item2,
                NumberOfMandates = election.NumberOfMandates.ToString(),
            },
            MajoralElection = new ElectionResultTypeMajoralElection
            {
                CountOfBlankVotesTotal = ResultDetailFromTotal(electionResult.EmptyVoteCount),
                CountOfInvalidVotesTotal = ResultDetailFromTotal(electionResult.InvalidVoteCount),
                Candidate = candidates,
                CountOfIndividualVotesTotal = !election.IndividualCandidatesDisabled
                    ? ResultDetailFromTotal(electionResult.IndividualVoteCount)
                    : null,
            },
        };
    }

    private static CandidateResultType ToEchCandidateResult(this MajorityElectionCandidateResultBase candidateResult, MajorityElectionCandidateBase candidate)
    {
        var candidateText = $"{candidate.PoliticalLastName} {candidate.PoliticalFirstName}";
        var texts = Languages.All
            .Select(l => new CandidateTextInformationTypeCandidateTextInfo
            {
                Language = l,
                CandidateText = candidateText,
            })
            .ToList();

        return new CandidateResultType
        {
            CountOfVotesTotal = candidateResult.VoteCount.ToString(CultureInfo.InvariantCulture),
            CandidateInformation = new CandidateInformationType
            {
                OfficialCandidateYesNo = true,
                CandidateIdentification = candidate.Id.ToString(),
                FamilyName = candidate.LastName,
                FirstName = candidate.FirstName,
                CallName = candidate.PoliticalFirstName,
                CandidateReference = candidate.Number,
                CandidateText = texts,
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
