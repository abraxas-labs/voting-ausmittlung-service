// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class DomainOfInfluenceCantonDefaultsBuilder
{
    private readonly CantonSettingsRepo _cantonSettingsRepo;
    private readonly DomainOfInfluenceRepo _doiRepo;
    private readonly DataContext _dataContext;

    public DomainOfInfluenceCantonDefaultsBuilder(
        CantonSettingsRepo cantonSettingsRepo,
        DomainOfInfluenceRepo doiRepo,
        DataContext dataContext)
    {
        _cantonSettingsRepo = cantonSettingsRepo;
        _doiRepo = doiRepo;
        _dataContext = dataContext;
    }

    public async Task BuildForDomainOfInfluence(DomainOfInfluence domainOfInfluence)
    {
        var cantonSettings = await LoadCantonSettings(domainOfInfluence.Canton);
        BuildCantonDefaultsOnDomainOfInfluence(cantonSettings, domainOfInfluence);
    }

    public async Task RebuildForCanton(CantonSettings cantonSettings)
        => await Rebuild(
            cantonSettings,
            doi => doi.Canton == cantonSettings.Canton);

    public async Task RebuildForRootDomainOfInfluenceCantonUpdate(DomainOfInfluence rootBasisDomainOfInfluence)
         => await Rebuild(
             await LoadCantonSettings(rootBasisDomainOfInfluence.Canton),
             doi => doi.Id == rootBasisDomainOfInfluence.Id);

    private async Task Rebuild(
        CantonSettings cantonSettings,
        Func<DomainOfInfluence, bool>? rootDoiPredicate = null)
    {
        var basisDomainOfInfluences = await _doiRepo.Query()
            .Where(x => x.SnapshotContestId == null)
            .ToListAsync();
        var basisTree = DomainOfInfluenceTreeBuilder.BuildTree(basisDomainOfInfluences);

        if (rootDoiPredicate != null)
        {
            basisTree = basisTree.Where(rootDoiPredicate).ToList();
        }

        var affectedBasisDoiIds = basisTree
            .Flatten(doi => doi.Children)
            .Select(doi => doi.Id)
            .ToList();

        var trackedDois = await _doiRepo.Query()
            .AsTracking()
            .Where(doi => affectedBasisDoiIds.Contains(doi.Id)
                || (affectedBasisDoiIds.Contains(doi.BasisDomainOfInfluenceId) && doi.SnapshotContest!.State <= ContestState.TestingPhase))
            .ToListAsync();

        foreach (var doi in trackedDois)
        {
            BuildCantonDefaultsOnDomainOfInfluence(cantonSettings, doi);
        }

        await _dataContext.SaveChangesAsync();
    }

    private void BuildCantonDefaultsOnDomainOfInfluence(CantonSettings cantonSettings, DomainOfInfluence domainOfInfluence)
    {
        domainOfInfluence.SwissAbroadVotingRight = GetSwissAbroadVotingRight(cantonSettings, domainOfInfluence.Type);
    }

    private SwissAbroadVotingRight GetSwissAbroadVotingRight(CantonSettings cantonSettings, DomainOfInfluenceType doiType)
    {
        return cantonSettings.SwissAbroadVotingRightDomainOfInfluenceTypes.Contains(doiType)
            ? cantonSettings.SwissAbroadVotingRight
            : SwissAbroadVotingRight.NoRights;
    }

    private async Task<CantonSettings> LoadCantonSettings(DomainOfInfluenceCanton canton)
    {
        return await _cantonSettingsRepo.GetByDomainOfInfluenceCanton(canton)
            ?? new CantonSettings();
    }
}
