// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.Snapshot;

public class ContestSnapshotBuilder
{
    private readonly DataContext _dbContext;

    public ContestSnapshotBuilder(DataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateSnapshotForContest(Contest contest)
    {
        var countingCircleBasisIdToNewIdMap = await CopyCountingCircles(contest.Id);
        var domainOfInfluenceBasisIdToNewIdMap = await CopyDomainOfInfluences(contest.Id);
        await _dbContext.SaveChangesAsync();

        await CopyDomainOfInfluenceCountingCircleRelations(countingCircleBasisIdToNewIdMap, domainOfInfluenceBasisIdToNewIdMap);

        contest.DomainOfInfluenceId = MapToNewId(domainOfInfluenceBasisIdToNewIdMap, contest.DomainOfInfluenceId);
        _dbContext.Contests.Update(contest);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<Dictionary<Guid, Guid>> CopyCountingCircles(Guid contestId)
    {
        var countingCircles = await _dbContext.CountingCircles
            .Include(cc => cc.ResponsibleAuthority)
            .Include(cc => cc.ContactPersonDuringEvent)
            .Include(cc => cc.ContactPersonAfterEvent)
            .Where(cc => cc.SnapshotContestId == null)
            .ToListAsync();

        foreach (var countingCircle in countingCircles)
        {
            countingCircle.SnapshotForContest(contestId);
        }

        _dbContext.CountingCircles.AddRange(countingCircles);
        return countingCircles.ToDictionary(doi => doi.BasisCountingCircleId, doi => doi.Id);
    }

    private async Task<Dictionary<Guid, Guid>> CopyDomainOfInfluences(Guid contestId)
    {
        var domainOfInfluences = await _dbContext.DomainOfInfluences
            .AsSplitQuery()
            .Include(x => x.PlausibilisationConfiguration)
                .ThenInclude(x => x!.ComparisonVoterParticipationConfigurations)
            .Include(x => x.PlausibilisationConfiguration)
                .ThenInclude(x => x!.ComparisonCountOfVotersConfigurations)
            .Include(x => x.PlausibilisationConfiguration)
                .ThenInclude(x => x!.ComparisonVotingChannelConfigurations)
            .Include(x => x.Parties)
                .ThenInclude(x => x.Translations)
            .Where(x => x.SnapshotContestId == null)
            .ToListAsync();

        foreach (var domainOfInfluence in domainOfInfluences)
        {
            domainOfInfluence.SnapshotForContest(contestId);
        }

        var domainOfInfluenceBasisIdToNewIdMap = domainOfInfluences.ToDictionary(doi => doi.BasisDomainOfInfluenceId, doi => doi.Id);

        foreach (var domainOfInfluence in domainOfInfluences.Where(doi => doi.ParentId != null))
        {
            domainOfInfluence.ParentId = MapToNewId(domainOfInfluenceBasisIdToNewIdMap, domainOfInfluence.ParentId!.Value);
        }

        _dbContext.DomainOfInfluences.AddRange(domainOfInfluences);
        return domainOfInfluenceBasisIdToNewIdMap;
    }

    private async Task CopyDomainOfInfluenceCountingCircleRelations(Dictionary<Guid, Guid> countingCircleBasisIdToNewIdMapping, Dictionary<Guid, Guid> doiBasisIdToNewIdMapping)
    {
        var doiCountingCircles = await _dbContext.DomainOfInfluenceCountingCircles
            .Where(x => x.DomainOfInfluence.SnapshotContestId == null)
            .ToListAsync();

        foreach (var doiCc in doiCountingCircles)
        {
            doiCc.Id = Guid.NewGuid();
            doiCc.DomainOfInfluenceId = MapToNewId(doiBasisIdToNewIdMapping, doiCc.DomainOfInfluenceId);
            doiCc.CountingCircleId = MapToNewId(countingCircleBasisIdToNewIdMapping, doiCc.CountingCircleId);
        }

        _dbContext.DomainOfInfluenceCountingCircles.AddRange(doiCountingCircles);
    }

    private Guid MapToNewId(Dictionary<Guid, Guid> basisIdToNewIdMapping, Guid basisId)
    {
        if (!basisIdToNewIdMapping.TryGetValue(basisId, out var newId))
        {
            throw new InvalidOperationException($"Cannot map {basisId} to a new id");
        }

        return newId;
    }
}
