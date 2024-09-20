// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ech0110_4_0;
using Ech0155_4_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class ProportionalElectionResultMapping
{
    internal static ElectionGroupResultsType ToEchElectionGroupResult(this ProportionalElectionResult electionResult)
    {
        return new ElectionGroupResultsType
        {
            DomainOfInfluenceIdentification = electionResult.ProportionalElection.DomainOfInfluence.BasisDomainOfInfluenceId.ToString(),
            ElectionResults = new List<ElectionResultType> { electionResult.ToEchElectionResult() },
            CountOfAccountedBallots = ResultDetailFromTotal(electionResult.CountOfVoters.TotalAccountedBallots),
            CountOfReceivedBallotsTotal = ResultDetailFromTotal(electionResult.CountOfVoters.TotalReceivedBallots),
            CountOfUnaccountedBallots = ResultDetailFromTotal(electionResult.CountOfVoters.TotalUnaccountedBallots),
            CountOfUnaccountedBlankBallots = ResultDetailFromTotal(electionResult.CountOfVoters.TotalBlankBallots),
            CountOfUnaccountedInvalidBallots = ResultDetailFromTotal(electionResult.CountOfVoters.TotalInvalidBallots),
            CountOfVotersInformation = new CountOfVotersInformationType { CountOfVotersTotal = electionResult.TotalCountOfVoters.ToString(CultureInfo.InvariantCulture) },
        };
    }

    private static ElectionResultType ToEchElectionResult(this ProportionalElectionResult electionResult)
    {
        var election = electionResult.ProportionalElection;

        return new ElectionResultType
        {
            Election = new ElectionType
            {
                ElectionIdentification = election.Id.ToString(),
                TypeOfElection = TypeOfElectionType.Item1,
                NumberOfMandates = election.NumberOfMandates.ToString(),
            },
            ProportionalElection = new ElectionResultTypeProportionalElection
            {
                Candidate = electionResult.ListResults
                    .SelectMany(l => l.CandidateResults)
                    .OrderBy(r => r.ListResult.List.Position)
                    .ThenBy(r => r.Candidate.Position)
                    .Select(c => c.ToEchCandidateResult())
                    .ToList(),
                List = electionResult.ListResults
                    .OrderBy(r => r.List.Position)
                    .Select(l => l.ToEchListResult())
                    .ToList(),
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
            .Select(t => new ListDescriptionInformationTypeListDescriptionInfo
            {
                Language = t.Language,
                ListDescription = t.Description,
                ListDescriptionShort = t.ShortDescription,
            })
            .ToList();
        var listInformation = new ListInformationType
        {
            ListIdentification = list.Id.ToString(),
            ListIndentureNumber = list.OrderNumber,
            ListDescription = listDescriptions,
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
            ListResults = new List<CandidateListResultType>
            {
                    new()
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
