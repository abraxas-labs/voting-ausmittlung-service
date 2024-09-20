// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
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
using Voting.Ausmittlung.Data.Utils;
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

        return PartialEndResultUtils.MergeIntoPartialEndResult(election, electionResults);
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
