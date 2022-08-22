// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class MajorityElectionEndResultReader
{
    private readonly IDbRepository<DataContext, MajorityElectionEndResult> _endResultRepo;
    private readonly IDbRepository<DataContext, ContestDomainOfInfluenceDetails> _contestDomainOfInfluenceDetailsRepo;
    private readonly PermissionService _permissionService;

    public MajorityElectionEndResultReader(
        IDbRepository<DataContext, MajorityElectionEndResult> endResultRepo,
        IDbRepository<DataContext, ContestDomainOfInfluenceDetails> contestDomainOfInfluenceDetailsRepo,
        PermissionService permissionService)
    {
        _endResultRepo = endResultRepo;
        _permissionService = permissionService;
        _contestDomainOfInfluenceDetailsRepo = contestDomainOfInfluenceDetailsRepo;
    }

    public async Task<MajorityElectionEndResult> GetEndResult(Guid majorityElectionId)
    {
        _permissionService.EnsureMonitoringElectionAdmin();
        var tenantId = _permissionService.TenantId;
        var majorityElectionEndResult = await _endResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.MajorityElection.Translations)
            .Include(x => x.MajorityElection.DomainOfInfluence)
            .Include(x => x.MajorityElection.Contest.Translations)
            .Include(x => x.MajorityElection.Contest.DomainOfInfluence)
            .Include(x => x.CandidateEndResults)
                .ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.SecondaryMajorityElectionEndResults)
                .ThenInclude(x => x.CandidateEndResults)
                    .ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.SecondaryMajorityElectionEndResults)
                .ThenInclude(x => x.SecondaryMajorityElection.Translations)
            .FirstOrDefaultAsync(x => x.MajorityElectionId == majorityElectionId && x.MajorityElection.DomainOfInfluence.SecureConnectId == tenantId)
            ?? throw new EntityNotFoundException(majorityElectionId);

        SortCandidateByRank(majorityElectionEndResult);

        majorityElectionEndResult.MajorityElection.DomainOfInfluence.Details = await _contestDomainOfInfluenceDetailsRepo.Query()
            .AsSplitQuery()
            .Include(d => d.CountOfVotersInformationSubTotals)
            .Include(d => d.VotingCards.Where(x => x.DomainOfInfluenceType == majorityElectionEndResult.MajorityElection.DomainOfInfluence.Type))
            .FirstOrDefaultAsync(x => x.DomainOfInfluenceId == majorityElectionEndResult.MajorityElection.DomainOfInfluenceId);
        majorityElectionEndResult.MajorityElection.DomainOfInfluence.Details?.OrderVotingCardsAndSubTotals();
        return majorityElectionEndResult;
    }

    public async Task<MajorityElectionEndResultAvailableLotDecisions> GetEndResultAvailableLotDecisions(Guid majorityElectionId)
    {
        _permissionService.EnsureMonitoringElectionAdmin();
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
