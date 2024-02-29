// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData.Mapping;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using DomainModels = Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ContestCountingCircleElectorateMockData
{
    public static readonly Guid GuidBundesurnengangGossau =
        AusmittlungUuidV5.BuildContestCountingCircleElectorate(
            Guid.Parse(ContestMockedData.IdBundesurnengang),
            CountingCircleMockedData.GuidGossau,
            new[] { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct, DomainOfInfluenceType.Bz });

    public static ContestCountingCircleElectorate BundesurnengangGossau
        => new ContestCountingCircleElectorate
        {
            Id = GuidBundesurnengangGossau,
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            DomainOfInfluenceTypes = new List<DomainOfInfluenceType>
            {
                DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct, DomainOfInfluenceType.Bz,
            },
        };

    public static IEnumerable<ContestCountingCircleElectorate> All
    {
        get { yield return BundesurnengangGossau; }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var electorates = All.ToList();

            var db = sp.GetRequiredService<DataContext>();

            foreach (var electorate in electorates)
            {
                electorate.CountingCircle = await db.CountingCircles.AsTracking().FirstAsync(cc =>
                    cc.BasisCountingCircleId == electorate.CountingCircleId &&
                    cc.SnapshotContestId == electorate.ContestId);
            }

            db.ContestCountingCircleElectorates.AddRange(electorates);
            await db.SaveChangesAsync();

            var electoratesByCcId = electorates
                .GroupBy(e => e.CountingCircle.Id)
                .ToDictionary(e => e.Key, e => e.ToList());

            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", "fake", "fake", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var mapper = sp.GetRequiredService<TestMapper>();

            foreach (var (_, groupedElectorates) in electoratesByCcId)
            {
                await aggregateRepository.Save(ToAggregate(
                    groupedElectorates,
                    groupedElectorates[0].ContestId,
                    groupedElectorates[0].CountingCircle.BasisCountingCircleId,
                    aggregateFactory,
                    mapper));
            }
        });
    }

    private static ContestCountingCircleElectoratesAggregate ToAggregate(
        IReadOnlyCollection<ContestCountingCircleElectorate> electorates,
        Guid contestId,
        Guid basisCountingCircleId,
        IAggregateFactory aggregateFactory,
        TestMapper mapper)
    {
        var aggregate = aggregateFactory.New<ContestCountingCircleElectoratesAggregate>();
        var domainElectorates = mapper.Map<List<DomainModels.ContestCountingCircleElectorate>>(electorates);

        aggregate.CreateFrom(domainElectorates, contestId, basisCountingCircleId);
        return aggregate;
    }
}
