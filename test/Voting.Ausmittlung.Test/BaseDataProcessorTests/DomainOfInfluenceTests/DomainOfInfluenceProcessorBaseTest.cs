// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.DomainOfInfluenceTests;

public abstract class DomainOfInfluenceProcessorBaseTest : BaseDataProcessorTest
{
    protected DomainOfInfluenceProcessorBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected async Task<List<DomainOfInfluence>> GetData(Expression<Func<DomainOfInfluence, bool>> predicate)
    {
        var data = await RunOnDb(db => db.DomainOfInfluences
            .AsSplitQuery()
            .Include(x => x.CountingCircles)
            .Include(x => x.PlausibilisationConfiguration)
                .ThenInclude(x => x!.ComparisonVoterParticipationConfigurations)
            .Include(x => x.PlausibilisationConfiguration)
                .ThenInclude(x => x!.ComparisonCountOfVotersConfigurations)
            .Include(x => x.PlausibilisationConfiguration)
                .ThenInclude(x => x!.ComparisonVotingChannelConfigurations)
            .Where(predicate)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.SnapshotContestId)
            .ToListAsync());

        foreach (var domainOfInfluence in data)
        {
            domainOfInfluence.CountingCircles = domainOfInfluence.CountingCircles
                .OrderBy(x => x.CountingCircleId)
                .ToList();

            foreach (var doiCc in domainOfInfluence.CountingCircles)
            {
                doiCc.Id = Guid.Empty;
            }

            domainOfInfluence.CantonDefaults.EnabledVotingCardChannels =
                domainOfInfluence.CantonDefaults.EnabledVotingCardChannels.OrderByPriority().ToList();

            foreach (var child in domainOfInfluence.CantonDefaults.EnabledVotingCardChannels)
            {
                child.Id = Guid.Empty;
            }

            if (domainOfInfluence.PlausibilisationConfiguration == null)
            {
                continue;
            }

            domainOfInfluence.PlausibilisationConfiguration.OrderChildrenConfigurations();
            domainOfInfluence.PlausibilisationConfiguration.Id = Guid.Empty;
            domainOfInfluence.PlausibilisationConfiguration.DomainOfInfluenceId = Guid.Empty;

            foreach (var child in domainOfInfluence.PlausibilisationConfiguration.ComparisonVoterParticipationConfigurations)
            {
                child.Id = Guid.Empty;
                child.PlausibilisationConfigurationId = Guid.Empty;
            }

            foreach (var child in domainOfInfluence.PlausibilisationConfiguration.ComparisonCountOfVotersConfigurations)
            {
                child.Id = Guid.Empty;
                child.PlausibilisationConfigurationId = Guid.Empty;
            }

            foreach (var child in domainOfInfluence.PlausibilisationConfiguration.ComparisonVotingChannelConfigurations)
            {
                child.Id = Guid.Empty;
                child.PlausibilisationConfigurationId = Guid.Empty;
            }
        }

        return data;
    }

    protected Task<List<DomainOfInfluence>> GetData()
        => GetData(_ => true);
}
