// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class ProportionalElectionUnionEndResultReader
{
    private readonly PermissionService _permissionService;
    private readonly IDbRepository<DataContext, ProportionalElectionResult> _electionResultRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionUnion> _unionRepo;

    public ProportionalElectionUnionEndResultReader(
        PermissionService permissionService,
        IDbRepository<DataContext, ProportionalElectionResult> electionResultRepo,
        IDbRepository<DataContext, ProportionalElectionUnion> unionRepo)
    {
        _permissionService = permissionService;
        _electionResultRepo = electionResultRepo;
        _unionRepo = unionRepo;
    }

    public async Task<ProportionalElectionUnionEndResult> GetEndResult(Guid unionId)
    {
        var tenantId = _permissionService.TenantId;

        var union = await _unionRepo.Query()
            .AsSplitQuery()
            .Where(u => u.Id == unionId && u.SecureConnectId == tenantId)
            .Include(u => u.Contest.Translations)
            .Include(u => u.Contest.DomainOfInfluence)
            .Include(u => u.Contest.CantonDefaults)
            .Include(u => u.EndResult)
            .Include(u => u.ProportionalElectionUnionEntries.OrderBy(e => e.ProportionalElection.PoliticalBusinessNumber))
            .ThenInclude(e => e.ProportionalElection.EndResult!.ListEndResults.OrderBy(x => x.List.OrderNumber))
            .ThenInclude(e => e.List.Translations)
            .Include(u => u.ProportionalElectionUnionEntries)
            .ThenInclude(e => e.ProportionalElection.Translations)
            .Include(u => u.ProportionalElectionUnionEntries)
            .ThenInclude(e => e.ProportionalElection.DomainOfInfluence)
            .FirstOrDefaultAsync();

        return union?.EndResult ?? throw new EntityNotFoundException(nameof(ProportionalElectionUnionEndResult), unionId);
    }

    public async Task<ProportionalElectionUnionEndResult> GetPartialEndResult(Guid unionId)
    {
        var union = await _unionRepo.Query()
            .AsSplitQuery()
            .Include(u => u.Contest.Translations)
            .Include(u => u.Contest.DomainOfInfluence)
            .Include(x => x.Contest.CantonDefaults)
            .FirstOrDefaultAsync(e => e.Id == unionId)
            ?? throw new EntityNotFoundException(unionId);
        var partialResultsCountingCircleIds = await _permissionService.GetViewablePartialResultsCountingCircleIds(union.ContestId);

        if (partialResultsCountingCircleIds.Count == 0)
        {
            throw new EntityNotFoundException(unionId);
        }

        var electionResults = await _electionResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.ProportionalElection)
            .ThenInclude(x => x.Translations)
            .Include(x => x.ProportionalElection)
            .ThenInclude(x => x.DomainOfInfluence)
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
            .Where(x => x.ProportionalElection.ProportionalElectionUnionEntries.Any(e => e.ProportionalElectionUnionId == unionId)
                && partialResultsCountingCircleIds.Contains(x.CountingCircleId))
            .ToListAsync();

        if (electionResults.Count == 0)
        {
            throw new EntityNotFoundException(unionId);
        }

        return MergeIntoPartialEndResult(union, electionResults);
    }

    private ProportionalElectionUnionEndResult MergeIntoPartialEndResult(ProportionalElectionUnion union, List<ProportionalElectionResult> results)
    {
        return new ProportionalElectionUnionEndResult
        {
            ProportionalElectionUnion = new ProportionalElectionUnion
            {
                Id = union.Id,
                ContestId = union.ContestId,
                Contest = union.Contest,
                SecureConnectId = union.SecureConnectId,
                Description = union.Description,
                ProportionalElectionUnionEntries = results
                    .GroupBy(x => x.ProportionalElectionId)
                    .Select(g => new ProportionalElectionUnionEntry
                    {
                        ProportionalElection = new ProportionalElection
                        {
                            EndResult = ProportionalElectionEndResultReader.MergeIntoPartialEndResult(g.First().ProportionalElection, g.ToList()),
                            Translations = g.First().ProportionalElection.Translations,
                        },
                    })
                    .ToList(),
            },
            TotalCountOfElections = results.DistinctBy(r => r.ProportionalElectionId).Count(),

            // Not enough information for these, just initialize them with the default value
            Finalized = false,
        };
    }
}
