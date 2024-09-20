// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Models;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Utils.LotDecisionBuilder;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class ProportionalElectionEndResultReader
{
    private readonly IDbRepository<DataContext, ProportionalElectionEndResult> _endResultRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionListEndResult> _listEndResultRepo;
    private readonly IDbRepository<DataContext, ProportionalElection> _electionRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionResult> _electionResultRepo;
    private readonly PermissionService _permissionService;

    public ProportionalElectionEndResultReader(
        IDbRepository<DataContext, ProportionalElectionEndResult> endResultRepo,
        IDbRepository<DataContext, ProportionalElectionListEndResult> listEndResultRepo,
        IDbRepository<DataContext, ProportionalElection> electionRepo,
        IDbRepository<DataContext, ProportionalElectionResult> electionResultRepo,
        PermissionService permissionService)
    {
        _endResultRepo = endResultRepo;
        _listEndResultRepo = listEndResultRepo;
        _electionRepo = electionRepo;
        _electionResultRepo = electionResultRepo;
        _permissionService = permissionService;
    }

    public async Task<ProportionalElectionEndResult> GetEndResult(Guid proportionalElectionId)
    {
        var tenantId = _permissionService.TenantId;
        var proportionalElectionEndResult = await _endResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.ProportionalElection.Translations)
            .Include(x => x.ProportionalElection.DomainOfInfluence)
            .Include(x => x.ProportionalElection.Contest.DomainOfInfluence)
            .Include(x => x.ProportionalElection.Contest.Translations)
            .Include(x => x.ProportionalElection.Contest.CantonDefaults)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.VotingCards)
            .Include(x => x.ListEndResults)
                .ThenInclude(x => x.CandidateEndResults)
                    .ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.ListEndResults)
                .ThenInclude(x => x.List.Translations)
            .Include(x => x.ListEndResults)
                .ThenInclude(x => x.List)
                    .ThenInclude(x => x.ProportionalElectionListUnionEntries)
                        .ThenInclude(x => x.ProportionalElectionListUnion.Translations)
            .FirstOrDefaultAsync(x => x.ProportionalElectionId == proportionalElectionId && x.ProportionalElection.DomainOfInfluence.SecureConnectId == tenantId)
            ?? throw new EntityNotFoundException(proportionalElectionId);

        OrderEntities(proportionalElectionEndResult, c => c.Rank);
        return proportionalElectionEndResult;
    }

    public async Task<ProportionalElectionListEndResultAvailableLotDecisions> GetEndResultAvailableLotDecisions(
        Guid proportionalElectionListId)
    {
        var tenantId = _permissionService.TenantId;

        var proportionalElectionListEndResult = await _listEndResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.List.Translations)
            .Include(x => x.ElectionEndResult.ProportionalElection.DomainOfInfluence)
            .Include(x => x.CandidateEndResults).ThenInclude(x => x.Candidate.Translations)
            .FirstOrDefaultAsync(x => x.ListId == proportionalElectionListId && x.ElectionEndResult.ProportionalElection.DomainOfInfluence.SecureConnectId == tenantId)
            ?? throw new EntityNotFoundException(proportionalElectionListId);

        var endResult = proportionalElectionListEndResult.ElectionEndResult;

        if (!endResult.AllCountingCirclesDone || !endResult.MandateDistributionTriggered)
        {
            throw new ValidationException("lot decisions are not allowed on this end result");
        }

        // candidate require a list for the description.
        // add list separately to simplify the query
        foreach (var candidateEndResult in proportionalElectionListEndResult.CandidateEndResults)
        {
            candidateEndResult.Candidate.ProportionalElectionList = proportionalElectionListEndResult.List;
        }

        return new ProportionalElectionListEndResultAvailableLotDecisionsBuilder()
            .BuildAvailableLotDecisions(proportionalElectionListEndResult);
    }

    public async Task<ProportionalElectionEndResult> GetPartialEndResult(Guid electionId)
    {
        var election = await _electionRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Translations)
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.Contest.DomainOfInfluence)
            .Include(x => x.Contest.Translations)
            .Include(x => x.Contest.CantonDefaults)
            .FirstOrDefaultAsync(e => e.Id == electionId)
            ?? throw new EntityNotFoundException(electionId);
        var partialResultsCountingCircleIds = await _permissionService.GetViewablePartialResultsCountingCircleIds(election.ContestId);

        if (partialResultsCountingCircleIds.Count == 0)
        {
            throw new EntityNotFoundException(electionId);
        }

        var electionResults = await _electionResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.CountingCircle.ContestDetails)
            .ThenInclude(x => x.VotingCards)
            .Include(x => x.CountingCircle.ContestDetails)
            .ThenInclude(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.ListResults)
            .ThenInclude(x => x.CandidateResults)
            .ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.ListResults)
            .ThenInclude(x => x.List.Translations)
            .Include(x => x.ListResults)
            .ThenInclude(x => x.List)
            .ThenInclude(x => x.ProportionalElectionListUnionEntries)
            .ThenInclude(x => x.ProportionalElectionListUnion.Translations)
            .Where(x => x.ProportionalElectionId == electionId && partialResultsCountingCircleIds.Contains(x.CountingCircleId))
            .ToListAsync();

        if (electionResults.Count == 0)
        {
            throw new EntityNotFoundException(electionId);
        }

        return MergeIntoPartialEndResult(election, electionResults);
    }

    internal static ProportionalElectionEndResult MergeIntoPartialEndResult(ProportionalElection election, List<ProportionalElectionResult> results)
    {
        var partialResult = new ProportionalElectionEndResult
        {
            ProportionalElection = election,
            ProportionalElectionId = election.Id,
            VotingCards = results
                .SelectMany(r => r.CountingCircle.ContestDetails.SelectMany(cc => cc.VotingCards))
                .GroupBy(vc => (vc.Channel, vc.Valid, vc.DomainOfInfluenceType))
                .Select(g => new ProportionalElectionEndResultVotingCardDetail
                {
                    Channel = g.Key.Channel,
                    Valid = g.Key.Valid,
                    DomainOfInfluenceType = g.Key.DomainOfInfluenceType,
                    CountOfReceivedVotingCards = g.Sum(x => x.CountOfReceivedVotingCards),
                })
                .ToList(),
            CountOfVotersInformationSubTotals = results
                .SelectMany(r => r.CountingCircle.ContestDetails.SelectMany(cc => cc.CountOfVotersInformationSubTotals))
                .GroupBy(cov => (cov.Sex, cov.VoterType))
                .Select(g => new ProportionalElectionEndResultCountOfVotersInformationSubTotal
                {
                    VoterType = g.Key.VoterType,
                    Sex = g.Key.Sex,
                    CountOfVoters = g.Sum(x => x.CountOfVoters),
                })
                .ToList(),
            CountOfVoters = new PoliticalBusinessCountOfVoters
            {
                ConventionalAccountedBallots = results.Sum(r => r.CountOfVoters.ConventionalAccountedBallots ?? 0),
                ConventionalBlankBallots = results.Sum(r => r.CountOfVoters.ConventionalBlankBallots ?? 0),
                ConventionalInvalidBallots = results.Sum(r => r.CountOfVoters.ConventionalInvalidBallots ?? 0),
                ConventionalReceivedBallots = results.Sum(r => r.CountOfVoters.ConventionalReceivedBallots ?? 0),
                EVotingAccountedBallots = results.Sum(r => r.CountOfVoters.EVotingAccountedBallots),
                EVotingBlankBallots = results.Sum(r => r.CountOfVoters.EVotingBlankBallots),
                EVotingInvalidBallots = results.Sum(r => r.CountOfVoters.EVotingInvalidBallots),
                EVotingReceivedBallots = results.Sum(r => r.CountOfVoters.EVotingReceivedBallots),
            },
            ConventionalSubTotal = new ProportionalElectionResultSubTotal
            {
                TotalCountOfModifiedLists = results.Sum(r => r.ConventionalSubTotal.TotalCountOfModifiedLists),
                TotalCountOfUnmodifiedLists = results.Sum(r => r.ConventionalSubTotal.TotalCountOfUnmodifiedLists),
                TotalCountOfListsWithoutParty = results.Sum(r => r.ConventionalSubTotal.TotalCountOfListsWithoutParty),
                TotalCountOfBlankRowsOnListsWithoutParty = results.Sum(r => r.ConventionalSubTotal.TotalCountOfBlankRowsOnListsWithoutParty),
            },
            EVotingSubTotal = new ProportionalElectionResultSubTotal
            {
                TotalCountOfModifiedLists = results.Sum(r => r.EVotingSubTotal.TotalCountOfModifiedLists),
                TotalCountOfUnmodifiedLists = results.Sum(r => r.EVotingSubTotal.TotalCountOfUnmodifiedLists),
                TotalCountOfListsWithoutParty = results.Sum(r => r.EVotingSubTotal.TotalCountOfListsWithoutParty),
                TotalCountOfBlankRowsOnListsWithoutParty = results.Sum(r => r.EVotingSubTotal.TotalCountOfBlankRowsOnListsWithoutParty),
            },
            TotalCountOfVoters = results.Sum(r => r.TotalCountOfVoters),
            CountOfDoneCountingCircles = results.Count(r => r.AuditedTentativelyTimestamp.HasValue),
            TotalCountOfCountingCircles = results.Count,
            ListEndResults = results
                .SelectMany(r => r.ListResults)
                .GroupBy(r => r.ListId)
                .Select(g => new ProportionalElectionListEndResult
                {
                    ListId = g.Key,
                    List = g.First().List,
                    ConventionalSubTotal = new ProportionalElectionListResultSubTotal
                    {
                        ModifiedListsCount = g.Sum(l => l.ConventionalSubTotal.ModifiedListsCount),
                        UnmodifiedListsCount = g.Sum(l => l.ConventionalSubTotal.UnmodifiedListsCount),
                        ModifiedListVotesCount = g.Sum(l => l.ConventionalSubTotal.ModifiedListVotesCount),
                        UnmodifiedListVotesCount = g.Sum(l => l.ConventionalSubTotal.UnmodifiedListVotesCount),
                        ModifiedListBlankRowsCount = g.Sum(l => l.ConventionalSubTotal.ModifiedListBlankRowsCount),
                        UnmodifiedListBlankRowsCount = g.Sum(l => l.ConventionalSubTotal.UnmodifiedListBlankRowsCount),
                        ListVotesCountOnOtherLists = g.Sum(l => l.ConventionalSubTotal.ListVotesCountOnOtherLists),
                    },
                    EVotingSubTotal = new ProportionalElectionListResultSubTotal
                    {
                        ModifiedListsCount = g.Sum(l => l.EVotingSubTotal.ModifiedListsCount),
                        UnmodifiedListsCount = g.Sum(l => l.EVotingSubTotal.UnmodifiedListsCount),
                        ModifiedListVotesCount = g.Sum(l => l.EVotingSubTotal.ModifiedListVotesCount),
                        UnmodifiedListVotesCount = g.Sum(l => l.EVotingSubTotal.UnmodifiedListVotesCount),
                        ModifiedListBlankRowsCount = g.Sum(l => l.EVotingSubTotal.ModifiedListBlankRowsCount),
                        UnmodifiedListBlankRowsCount = g.Sum(l => l.EVotingSubTotal.UnmodifiedListBlankRowsCount),
                        ListVotesCountOnOtherLists = g.Sum(l => l.EVotingSubTotal.ListVotesCountOnOtherLists),
                    },
                    CandidateEndResults = g
                        .SelectMany(x => x.CandidateResults)
                        .GroupBy(x => x.CandidateId)
                        .Select(x => new ProportionalElectionCandidateEndResult
                        {
                            CandidateId = x.Key,
                            Candidate = x.First().Candidate,
                            State = ProportionalElectionCandidateEndResultState.Pending,
                            ConventionalSubTotal = new ProportionalElectionCandidateResultSubTotal
                            {
                                ModifiedListVotesCount = x.Sum(c => c.ConventionalSubTotal.ModifiedListVotesCount),
                                UnmodifiedListVotesCount = x.Sum(c => c.ConventionalSubTotal.UnmodifiedListVotesCount),
                                CountOfVotesFromAccumulations = x.Sum(c => c.ConventionalSubTotal.CountOfVotesFromAccumulations),
                                CountOfVotesOnOtherLists = x.Sum(c => c.ConventionalSubTotal.CountOfVotesOnOtherLists),
                            },
                            EVotingSubTotal = new ProportionalElectionCandidateResultSubTotal
                            {
                                ModifiedListVotesCount = x.Sum(c => c.EVotingSubTotal.ModifiedListVotesCount),
                                UnmodifiedListVotesCount = x.Sum(c => c.EVotingSubTotal.UnmodifiedListVotesCount),
                                CountOfVotesFromAccumulations = x.Sum(c => c.EVotingSubTotal.CountOfVotesFromAccumulations),
                                CountOfVotesOnOtherLists = x.Sum(c => c.EVotingSubTotal.CountOfVotesOnOtherLists),
                            },
                            VoteCount = x.Sum(c => c.VoteCount),
                        })
                        .ToList(),
                })
                .ToList(),

            // Not enough information for these, just initialize them with the default value
            Finalized = false,
        };

        partialResult.CountOfVoters.UpdateVoterParticipation(partialResult.TotalCountOfVoters);
        OrderEntities(partialResult, c => c.Candidate.Position);
        return partialResult;
    }

    private static void OrderEntities(ProportionalElectionEndResult endResult, Func<ProportionalElectionCandidateEndResult, int> candidateOrderFunc)
    {
        endResult.OrderVotingCardsAndSubTotals();
        endResult.ListEndResults = endResult.ListEndResults
            .OrderBy(x => x.List.Position)
            .ToList();

        foreach (var listEndResult in endResult.ListEndResults)
        {
            listEndResult.CandidateEndResults = listEndResult.CandidateEndResults
                .OrderBy(candidateOrderFunc)
                .ThenBy(x => x.CandidateId)
                .ToList();

            // candidate require a list for the description.
            // add list separately to simplify the query
            foreach (var candidateEndResult in listEndResult.CandidateEndResults)
            {
                candidateEndResult.Candidate.ProportionalElectionList = listEndResult.List;
            }
        }
    }
}
