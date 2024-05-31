// (c) Copyright 2024 by Abraxas Informatik AG
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

public class MajorityElectionEndResultReader
{
    private readonly IDbRepository<DataContext, MajorityElectionEndResult> _endResultRepo;
    private readonly IDbRepository<DataContext, MajorityElection> _electionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResult> _electionResultRepo;
    private readonly PermissionService _permissionService;

    public MajorityElectionEndResultReader(
        IDbRepository<DataContext, MajorityElectionEndResult> endResultRepo,
        IDbRepository<DataContext, MajorityElection> electionRepo,
        IDbRepository<DataContext, MajorityElectionResult> electionResultRepo,
        PermissionService permissionService)
    {
        _endResultRepo = endResultRepo;
        _permissionService = permissionService;
        _electionRepo = electionRepo;
        _electionResultRepo = electionResultRepo;
    }

    public async Task<MajorityElectionEndResult> GetEndResult(Guid majorityElectionId)
    {
        var tenantId = _permissionService.TenantId;
        var majorityElectionEndResult = await _endResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.MajorityElection.Translations)
            .Include(x => x.MajorityElection.DomainOfInfluence)
            .Include(x => x.MajorityElection.Contest.Translations)
            .Include(x => x.MajorityElection.Contest.DomainOfInfluence)
            .Include(x => x.MajorityElection.Contest.CantonDefaults)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.VotingCards)
            .Include(x => x.CandidateEndResults)
                .ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.SecondaryMajorityElectionEndResults)
                .ThenInclude(x => x.CandidateEndResults)
                    .ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.SecondaryMajorityElectionEndResults)
                .ThenInclude(x => x.SecondaryMajorityElection.Translations)
            .FirstOrDefaultAsync(x => x.MajorityElectionId == majorityElectionId && x.MajorityElection.DomainOfInfluence.SecureConnectId == tenantId)
            ?? throw new EntityNotFoundException(majorityElectionId);

        majorityElectionEndResult.OrderVotingCardsAndSubTotals();
        SortCandidateByRank(majorityElectionEndResult);

        return majorityElectionEndResult;
    }

    public async Task<MajorityElectionEndResultAvailableLotDecisions> GetEndResultAvailableLotDecisions(Guid majorityElectionId)
    {
        var tenantId = _permissionService.TenantId;

        var majorityElectionEndResult = await _endResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.MajorityElection.Translations)
            .Include(x => x.MajorityElection.DomainOfInfluence)
            .Include(x => x.CandidateEndResults).ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.SecondaryMajorityElectionEndResults).ThenInclude(x => x.CandidateEndResults).ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.SecondaryMajorityElectionEndResults).ThenInclude(x => x.SecondaryMajorityElection.Translations)
            .FirstOrDefaultAsync(x => x.MajorityElectionId == majorityElectionId && x.MajorityElection.DomainOfInfluence.SecureConnectId == tenantId)
            ?? throw new EntityNotFoundException(majorityElectionId);

        if (!majorityElectionEndResult.AllCountingCirclesDone)
        {
            throw new ValidationException("lot decisions are not allowed on this end result");
        }

        return new MajorityElectionEndResultAvailableLotDecisionsBuilder()
            .BuildAvailableLotDecisions(majorityElectionEndResult);
    }

    public async Task<MajorityElectionEndResult> GetPartialEndResult(Guid electionId)
    {
        var election = await _electionRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Translations)
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.Contest.DomainOfInfluence)
            .Include(x => x.Contest.Translations)
            .Include(x => x.Contest.CantonDefaults)
            .Include(x => x.SecondaryMajorityElections)
            .ThenInclude(x => x.Translations)
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
            .Include(x => x.CandidateResults)
            .ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.SecondaryMajorityElectionResults)
            .ThenInclude(x => x.CandidateResults)
            .ThenInclude(x => x.Candidate.Translations)
            .Where(x => x.MajorityElectionId == electionId && partialResultsCountingCircleIds.Contains(x.CountingCircleId))
            .ToListAsync();

        if (electionResults.Count == 0)
        {
            throw new EntityNotFoundException(electionId);
        }

        return MergeIntoPartialEndResult(election, electionResults);
    }

    private MajorityElectionEndResult MergeIntoPartialEndResult(MajorityElection election, List<MajorityElectionResult> results)
    {
        var partialResult = new MajorityElectionEndResult
        {
            MajorityElection = election,
            MajorityElectionId = election.Id,
            VotingCards = results
                .SelectMany(r => r.CountingCircle.ContestDetails.SelectMany(cc => cc.VotingCards))
                .GroupBy(vc => (vc.Channel, vc.Valid, vc.DomainOfInfluenceType))
                .Select(g => new MajorityElectionEndResultVotingCardDetail
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
                .Select(g => new MajorityElectionEndResultCountOfVotersInformationSubTotal
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
            ConventionalSubTotal = new MajorityElectionResultSubTotal
            {
                IndividualVoteCount = results.Sum(r => r.ConventionalSubTotal.IndividualVoteCount ?? 0),
                InvalidVoteCount = results.Sum(r => r.ConventionalSubTotal.InvalidVoteCount ?? 0),
                EmptyVoteCountWriteIns = results.Sum(r => r.ConventionalSubTotal.EmptyVoteCountWriteIns ?? 0),
                EmptyVoteCountExclWriteIns = results.Sum(r => r.ConventionalSubTotal.EmptyVoteCountExclWriteIns ?? 0),
                TotalCandidateVoteCountExclIndividual = results.Sum(r => r.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual),
            },
            EVotingSubTotal = new MajorityElectionResultSubTotal
            {
                IndividualVoteCount = results.Sum(r => r.EVotingSubTotal.IndividualVoteCount),
                InvalidVoteCount = results.Sum(r => r.EVotingSubTotal.InvalidVoteCount),
                EmptyVoteCountWriteIns = results.Sum(r => r.EVotingSubTotal.EmptyVoteCountWriteIns),
                EmptyVoteCountExclWriteIns = results.Sum(r => r.EVotingSubTotal.EmptyVoteCountExclWriteIns),
                TotalCandidateVoteCountExclIndividual = results.Sum(r => r.EVotingSubTotal.TotalCandidateVoteCountExclIndividual),
            },
            TotalCountOfVoters = results.Sum(r => r.TotalCountOfVoters),
            CountOfDoneCountingCircles = results.Count(r => r.AuditedTentativelyTimestamp.HasValue),
            TotalCountOfCountingCircles = results.Count,
            CandidateEndResults = results
                .SelectMany(r => r.CandidateResults)
                .GroupBy(r => r.CandidateId)
                .Select(g => new MajorityElectionCandidateEndResult
                {
                    Candidate = g.First().Candidate,
                    CandidateId = g.First().CandidateId,
                    State = MajorityElectionCandidateEndResultState.Pending,
                    VoteCount = g.Sum(c => c.VoteCount),
                    ConventionalVoteCount = g.Sum(c => c.ConventionalVoteCount ?? 0),
                    EVotingVoteCount = g.Sum(c => c.EVotingInclWriteInsVoteCount),
                })
                .OrderByDescending(x => x.VoteCount)
                .ThenBy(x => x.Candidate.Position)
                .ToList(),
            SecondaryMajorityElectionEndResults = election.SecondaryMajorityElections
                .Select(se => MergeIntoPartialEndResult(se, results))
                .OrderBy(x => x.SecondaryMajorityElection.PoliticalBusinessNumber)
                .ThenBy(x => x.SecondaryMajorityElection.ShortDescription)
                .ToList(),

            // Not enough information for these, just initialize them with the default value
            Finalized = false,
            Calculation = new MajorityElectionEndResultCalculation(),
        };

        partialResult.CountOfVoters.UpdateVoterParticipation(partialResult.TotalCountOfVoters);
        partialResult.OrderVotingCardsAndSubTotals();
        return partialResult;
    }

    private SecondaryMajorityElectionEndResult MergeIntoPartialEndResult(SecondaryMajorityElection election, List<MajorityElectionResult> results)
    {
        var relevantResult = results
            .SelectMany(r => r.SecondaryMajorityElectionResults)
            .Where(ser => ser.SecondaryMajorityElectionId == election.Id)
            .ToList();
        var partialResult = new SecondaryMajorityElectionEndResult
        {
            SecondaryMajorityElection = election,
            SecondaryMajorityElectionId = election.Id,
            CandidateEndResults = relevantResult
                .SelectMany(r => r.CandidateResults)
                .GroupBy(r => r.CandidateId)
                .Select(g => new SecondaryMajorityElectionCandidateEndResult
                {
                    Candidate = g.First().Candidate,
                    CandidateId = g.First().CandidateId,
                    State = MajorityElectionCandidateEndResultState.Pending,
                    VoteCount = g.Sum(c => c.VoteCount),
                    ConventionalVoteCount = g.Sum(c => c.ConventionalVoteCount ?? 0),
                    EVotingVoteCount = g.Sum(c => c.EVotingInclWriteInsVoteCount),
                })
                .OrderByDescending(x => x.VoteCount)
                .ThenBy(x => x.Candidate.Position)
                .ToList(),
            ConventionalSubTotal = new MajorityElectionResultSubTotal
            {
                IndividualVoteCount = relevantResult.Sum(r => r.ConventionalSubTotal.IndividualVoteCount ?? 0),
                InvalidVoteCount = relevantResult.Sum(r => r.ConventionalSubTotal.InvalidVoteCount ?? 0),
                EmptyVoteCountWriteIns = relevantResult.Sum(r => r.ConventionalSubTotal.EmptyVoteCountWriteIns ?? 0),
                TotalCandidateVoteCountExclIndividual = relevantResult.Sum(r => r.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual),
                EmptyVoteCountExclWriteIns = relevantResult.Sum(r => r.ConventionalSubTotal.EmptyVoteCountExclWriteIns ?? 0),
            },
            EVotingSubTotal = new MajorityElectionResultSubTotal
            {
                IndividualVoteCount = relevantResult.Sum(r => r.EVotingSubTotal.IndividualVoteCount),
                InvalidVoteCount = relevantResult.Sum(r => r.EVotingSubTotal.InvalidVoteCount),
                EmptyVoteCountWriteIns = relevantResult.Sum(r => r.EVotingSubTotal.EmptyVoteCountWriteIns),
                TotalCandidateVoteCountExclIndividual = relevantResult.Sum(r => r.EVotingSubTotal.TotalCandidateVoteCountExclIndividual),
                EmptyVoteCountExclWriteIns = relevantResult.Sum(r => r.EVotingSubTotal.EmptyVoteCountExclWriteIns),
            },
        };

        return partialResult;
    }

    private void SortCandidateByRank(MajorityElectionEndResult endResult)
    {
        endResult.CandidateEndResults = endResult.CandidateEndResults
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.CandidateId)
            .ToList();

        endResult.SecondaryMajorityElectionEndResults = endResult.SecondaryMajorityElectionEndResults
            .OrderBy(x => x.SecondaryMajorityElection.PoliticalBusinessNumber)
            .ThenBy(x => x.SecondaryMajorityElection.ShortDescription)
            .ToList();

        foreach (var secondaryMajorityElectionEndResults in endResult.SecondaryMajorityElectionEndResults)
        {
            secondaryMajorityElectionEndResults.CandidateEndResults = secondaryMajorityElectionEndResults.CandidateEndResults
                .OrderBy(x => x.Rank)
                .ThenBy(x => x.CandidateId)
                .ToList();
        }
    }
}
