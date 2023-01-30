// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Globalization;
using System.Linq;
using eCH_0110_4_0;
using eCH_0155_4_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using ProportionalElection = eCH_0110_4_0.ProportionalElection;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class ProportionalElectionResultMapping
{
    internal static ElectionGroupResultsType ToEchElectionGroupResult(this ProportionalElectionResult electionResult)
    {
        return new ElectionGroupResultsType
        {
            DomainOfInfluenceIdentification = electionResult.ProportionalElection.DomainOfInfluence.BasisDomainOfInfluenceId.ToString(),
            ElectionResults = new[] { electionResult.ToEchElectionResult() },
            CountOfAccountedBallots = ResultDetailFromTotal(electionResult.CountOfVoters.TotalAccountedBallots),
            CountOfReceivedBallotsTotal = ResultDetailFromTotal(electionResult.CountOfVoters.TotalReceivedBallots),
            CountOfUnaccountedBallots = ResultDetailFromTotal(electionResult.CountOfVoters.TotalUnaccountedBallots),
            CountOfUnaccountedBlankBallots = ResultDetailFromTotal(electionResult.CountOfVoters.ConventionalBlankBallots.GetValueOrDefault()),
            CountOfUnaccountedInvalidBallots = ResultDetailFromTotal(electionResult.CountOfVoters.TotalInvalidBallots),
            CountOfVotersInformation = new CountOfVotersInformationType { CountOfVotersTotal = electionResult.TotalCountOfVoters.ToString(CultureInfo.InvariantCulture) },
        };
    }

    private static ElectionResultType ToEchElectionResult(this ProportionalElectionResult electionResult)
    {
        var election = electionResult.ProportionalElection;

        return new ElectionResultType
        {
            Election = ElectionType.Create(election.Id.ToString(), TypeOfElectionType.Proporz, election.NumberOfMandates),
            Item = new ProportionalElection
            {
                Candidate = electionResult.ListResults
                    .SelectMany(l => l.CandidateResults)
                    .OrderBy(r => r.ListResult.List.Position)
                    .ThenBy(r => r.Candidate.Position)
                    .Select(c => c.ToEchCandidateResult())
                    .ToArray(),
                List = electionResult.ListResults
                    .OrderBy(r => r.List.Position)
                    .Select(l => l.ToEchListResult())
                    .ToArray(),
                CountOfEmptyVotesOfChangedBallotsWithoutPartyAffiliation = ResultDetailFromTotal(electionResult.TotalCountOfBlankRowsOnListsWithoutParty),
                CountOfChangedBallotsWithoutPartyAffiliation = ResultDetailFromTotal(electionResult.TotalCountOfListsWithoutParty),
                CountOfChangedBallotsWithPartyAffiliation = ResultDetailFromTotal(electionResult.TotalCountOfModifiedLists),
            },
        };
    }

    private static ListResultsType ToEchListResult(this ProportionalElectionListResult listResult)
    {
        var list = listResult.List;
        var listDescriptions = list.Translations
            .Select(t => ListDescriptionInfo.Create(t.Language, t.Description, t.ShortDescription))
            .ToList();
        var listInformation = new ListInformationType
        {
            ListIdentification = list.Id.ToString(),
            ListIndentureNumber = list.OrderNumber,
            ListDescription = ListDescriptionInformation.Create(listDescriptions),
        };

        return new ListResultsType
        {
            ListInformation = listInformation,
            CountOfAdditionalVotes = ResultDetailFromTotal(listResult.BlankRowsCount),
            CountOfCandidateVotes = ResultDetailFromTotal(listResult.ListVotesCount),
            CountOfPartyVotes = ResultDetailFromTotal(listResult.TotalVoteCount),
            CountOfChangedBallots = ResultDetailFromTotal(listResult.ModifiedListsCount),
            CountOfUnchangedBallots = ResultDetailFromTotal(listResult.UnmodifiedListsCount),
        };
    }

    private static CandidateResultType ToEchCandidateResult(this ProportionalElectionCandidateResult candidateResult)
    {
        var candidate = candidateResult.Candidate;
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
            ListResults = new[]
            {
                    new CandidateListResultType
                    {
                        ListIdentification = candidate.ProportionalElectionListId.ToString(),
                        CountOfvotesFromChangedBallots = ResultDetailFromTotal(candidateResult.ModifiedListVotesCount),
                        CountOfvotesFromUnchangedBallots = ResultDetailFromTotal(candidateResult.UnmodifiedListVotesCount),
                    },
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
