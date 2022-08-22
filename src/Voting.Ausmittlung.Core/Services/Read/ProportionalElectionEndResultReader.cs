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

public class ProportionalElectionEndResultReader
{
    private readonly IDbRepository<DataContext, ProportionalElectionEndResult> _endResultRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionListEndResult> _listEndResultRepo;
    private readonly IDbRepository<DataContext, ContestDomainOfInfluenceDetails> _contestDomainOfInfluenceDetailsRepo;
    private readonly PermissionService _permissionService;

    public ProportionalElectionEndResultReader(
        IDbRepository<DataContext, ProportionalElectionEndResult> endResultRepo,
        IDbRepository<DataContext, ProportionalElectionListEndResult> listEndResultRepo,
        IDbRepository<DataContext, ContestDomainOfInfluenceDetails> contestDomainOfInfluenceDetailsRepo,
        PermissionService permissionService)
    {
        _endResultRepo = endResultRepo;
        _listEndResultRepo = listEndResultRepo;
        _permissionService = permissionService;
        _contestDomainOfInfluenceDetailsRepo = contestDomainOfInfluenceDetailsRepo;
    }

    public async Task<ProportionalElectionEndResult> GetEndResult(Guid proportionalElectionId)
    {
        _permissionService.EnsureMonitoringElectionAdmin();
        var tenantId = _permissionService.TenantId;
        var proportionalElectionEndResult = await _endResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.ProportionalElection.Translations)
            .Include(x => x.ProportionalElection.DomainOfInfluence)
            .Include(x => x.ProportionalElection.Contest.DomainOfInfluence)
            .Include(x => x.ProportionalElection.Contest.Translations)
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

        proportionalElectionEndResult.ListEndResults = proportionalElectionEndResult.ListEndResults
            .OrderBy(x => x.List.Position)
            .ToList();

        foreach (var listEndResult in proportionalElectionEndResult.ListEndResults)
        {
            listEndResult.CandidateEndResults = listEndResult.CandidateEndResults
                .OrderBy(x => x.Rank)
                .ThenBy(x => x.CandidateId)
                .ToList();

            // candidate require a list for the description.
            // add list seperately to simplify the query
            foreach (var candidateEndResult in listEndResult.CandidateEndResults)
            {
                candidateEndResult.Candidate.ProportionalElectionList = listEndResult.List;
            }
        }

        proportionalElectionEndResult.ProportionalElection.DomainOfInfluence.Details = await _contestDomainOfInfluenceDetailsRepo.Query()
            .AsSplitQuery()
            .Include(d => d.CountOfVotersInformationSubTotals)
            .Include(d => d.VotingCards.Where(x => x.DomainOfInfluenceType == proportionalElectionEndResult.ProportionalElection.DomainOfInfluence.Type))
            .FirstOrDefaultAsync(x => x.DomainOfInfluenceId == proportionalElectionEndResult.ProportionalElection.DomainOfInfluenceId);
        proportionalElectionEndResult.ProportionalElection.DomainOfInfluence.Details?.OrderVotingCardsAndSubTotals();
        return proportionalElectionEndResult;
    }

    public async Task<ProportionalElectionListEndResultAvailableLotDecisions> GetEndResultAvailableLotDecisions(
        Guid proportionalElectionListId)
    {
        _permissionService.EnsureMonitoringElectionAdmin();
        var tenantId = _permissionService.TenantId;

        var proportionalElectionListEndResult = await _listEndResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.List.Translations)
            .Include(x => x.ElectionEndResult.ProportionalElection.DomainOfInfluence)
            .Include(x => x.CandidateEndResults).ThenInclude(x => x.Candidate.Translations)
            .FirstOrDefaultAsync(x => x.ListId == proportionalElectionListId && x.ElectionEndResult.ProportionalElection.DomainOfInfluence.SecureConnectId == tenantId)
            ?? throw new EntityNotFoundException(proportionalElectionListId);

        if (!proportionalElectionListEndResult.ElectionEndResult.AllCountingCirclesDone)
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
}
