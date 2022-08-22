// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Queries;
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
            await _doiRepo.Query().WhereContestIsInTestingPhaseOrNoContest().ToListAsync(),
            doi => doi.Canton == cantonSettings.Canton);

    public async Task RebuildForRootDomainOfInfluenceCantonUpdate(DomainOfInfluence rootDomainOfInfluence, List<DomainOfInfluence> allDomainOfInfluences)
         => await Rebuild(
             await LoadCantonSettings(rootDomainOfInfluence.Canton),
             allDomainOfInfluences,
             doi => doi.Id == rootDomainOfInfluence.Id);

    private async Task Rebuild(CantonSettings cantonSettings, List<DomainOfInfluence> allDomainOfInfluences, Func<DomainOfInfluence, bool>? rootDoiPredicate = null)
    {
        var tree = DomainOfInfluenceTreeBuilder.BuildTree(allDomainOfInfluences);

        if (rootDoiPredicate != null)
        {
            tree = tree.Where(rootDoiPredicate).ToList();
        }

        var untrackedAffectedDois = tree.Flatten(doi => doi.Children).ToList();
        var affectedDoiIds = untrackedAffectedDois.ConvertAll(doi => doi.Id);

        var trackedDois = await _doiRepo.Query()
            .AsTracking()
            .Where(doi => affectedDoiIds.Contains(doi.Id))
            .ToListAsync();

        foreach (var doi in trackedDois)
        {
            BuildCantonDefaultsOnDomainOfInfluence(cantonSettings, doi);
        }

        await _dataContext.SaveChangesAsync();
    }

    private void BuildCantonDefaultsOnDomainOfInfluence(CantonSettings cantonSettings, DomainOfInfluence domainOfInfluence)
    {
        domainOfInfluence.CantonDefaults = new DomainOfInfluenceCantonDefaults
        {
            ProportionalElectionMandateAlgorithms = cantonSettings.ProportionalElectionMandateAlgorithms,
            MajorityElectionAbsoluteMajorityAlgorithm = cantonSettings.MajorityElectionAbsoluteMajorityAlgorithm,
            MajorityElectionInvalidVotes = cantonSettings.MajorityElectionInvalidVotes,
            SwissAbroadVotingRight = GetSwissAbroadVotingRight(cantonSettings, domainOfInfluence.Type),
            EnabledVotingCardChannels = cantonSettings.EnabledVotingCardChannels
                .ConvertAll(x => new DomainOfInfluenceCantonDefaultsVotingCardChannel { Valid = x.Valid, VotingChannel = x.VotingChannel }),
        };
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
