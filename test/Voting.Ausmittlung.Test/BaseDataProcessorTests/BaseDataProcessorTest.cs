// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.EventSignature;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Database.Models;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Models;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using User = Voting.Lib.Iam.Models.User;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests;

public abstract class BaseDataProcessorTest : BaseTest<TestApplicationFactory, TestStartup>
{
    private int _currentEventNumber;

    protected BaseDataProcessorTest(TestApplicationFactory factory)
        : base(factory)
    {
        ResetDb();

        TestEventPublisher = GetService<TestEventPublisher>();
        EventPublisherMock = GetService<EventPublisherMock>();
        EventPublisherMock.Clear();

        ContestCache = GetService<ContestCache>();
    }

    protected EventPublisherMock EventPublisherMock { get; }

    protected TestEventPublisher TestEventPublisher { get; }

    protected ContestCache ContestCache { get; }

    protected Task RunOnDb(Func<DataContext, Task> action, string? language = null)
    {
        return RunScoped<DataContext>(db =>
        {
            db.Language = language;
            return action(db);
        });
    }

    protected Task<TResult> RunOnDb<TResult>(Func<DataContext, Task<TResult>> action, string? language = null)
    {
        return RunScoped<DataContext, TResult>(db =>
        {
            db.Language = language;
            return action(db);
        });
    }

    protected void ResetDb()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var temporaryDb = scope.ServiceProvider.GetRequiredService<TemporaryDataContext>();
        DatabaseUtil.Truncate(db, temporaryDb);
    }

    protected Task ModifyDbEntities<T>(Expression<Func<T, bool>> predicate, Action<T> modifier, bool splitQuery = false)
        where T : class
    {
        return RunOnDb(async db =>
        {
            var query = db.Set<T>().AsTracking();
            if (splitQuery)
            {
                query = query.AsSplitQuery();
            }

            var entities = await query.Where(predicate).ToListAsync();
            foreach (var entity in entities)
            {
                modifier(entity);
            }

            await db.SaveChangesAsync();
        });
    }

    protected int GetNextEventNumber()
    {
        return _currentEventNumber++;
    }

    protected void SetDynamicIdToDefaultValue(IEnumerable<BaseEntity> entities)
    {
        foreach (var entity in entities)
        {
            entity.Id = Guid.Empty;
        }
    }

    protected void RemoveDynamicData(PoliticalBusiness politicalBusiness)
    {
        politicalBusiness.DomainOfInfluenceId = Guid.Empty;
        foreach (var translation in politicalBusiness.PoliticalBusinessTranslations)
        {
            translation.Id = Guid.Empty;
        }
    }

    protected void RemoveDynamicData(IEnumerable<MajorityElection> majorityElections)
    {
        foreach (var election in majorityElections)
        {
            RemoveDynamicData(election);

            foreach (var secondaryElection in election.SecondaryMajorityElections)
            {
                foreach (var translation in secondaryElection.Translations)
                {
                    translation.Id = Guid.Empty;
                }
            }

            foreach (var secondaryElection in election.SecondaryMajorityElectionsOnSeparateBallots)
            {
                RemoveDynamicData(secondaryElection);
            }
        }
    }

    protected void RemoveDynamicData(IEnumerable<PoliticalBusiness> pbs)
    {
        foreach (var politicalBusiness in pbs)
        {
            RemoveDynamicData(politicalBusiness);
        }
    }

    protected EventInfo GetMockedEventInfo()
    {
        return new()
        {
            Timestamp = new Timestamp
            {
                Seconds = 1594980476,
            },
            Tenant = ToEventInfoTenant(SecureConnectTestDefaults.MockedTenantDefault),
            User = ToEventInfoUser(SecureConnectTestDefaults.MockedUserDefault),
        };
    }

    protected void EnsureValidAggregatedVotingCards<T>(ICollection<T> votingCardsBefore, ICollection<T> votingCardsAfter, Func<T, bool> predicate, int diff)
        where T : AggregatedVotingCardResultDetail
    {
        (votingCardsAfter.Single(predicate).CountOfReceivedVotingCards - votingCardsBefore.Single(predicate).CountOfReceivedVotingCards).Should().Be(diff);
    }

    protected void EnsureValidAggregatedSubTotals<T>(ICollection<T> votingCardsBefore, ICollection<T> votingCardsAfter, Func<T, bool> predicate, int diff)
        where T : AggregatedCountOfVotersInformationSubTotal
    {
        (votingCardsAfter.Single(predicate).CountOfVoters - votingCardsBefore.Single(predicate).CountOfVoters).Should().Be(diff);
    }

    private EventInfoTenant ToEventInfoTenant(Tenant tenant)
    {
        return new EventInfoTenant
        {
            Id = tenant.Id,
            Name = tenant.Name,
        };
    }

    private EventInfoUser ToEventInfoUser(User user)
    {
        return new()
        {
            Id = user.Loginid,
            FirstName = user.Firstname ?? string.Empty,
            LastName = user.Lastname ?? string.Empty,
            Username = user.Username ?? string.Empty,
        };
    }
}
