// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.EventSignature;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Database.Models;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Authorization;
using Voting.Lib.Iam.Models;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Xunit;

namespace Voting.Ausmittlung.Test;

public abstract class BaseIntegrationTest : BaseTest<TestApplicationFactory, TestStartup>
{
    protected BaseIntegrationTest(TestApplicationFactory factory)
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

    protected void ResetDb()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var temporaryDb = scope.ServiceProvider.GetRequiredService<TemporaryDataContext>();
        DatabaseUtil.Truncate(db, temporaryDb);
    }

    protected void SetDynamicIdToDefaultValue(IEnumerable<BaseEntity> entities)
    {
        foreach (var entity in entities)
        {
            entity.Id = Guid.Empty;
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

    protected void TrySetFakeAuth(string tenantId, params string[] roles)
    {
        if (GetService<IAuth>().IsAuthenticated)
        {
            return;
        }

        GetService<IAuthStore>().SetValues(
            "mock-token",
            "fake",
            tenantId,
            roles,
            GetService<IPermissionProvider>().GetPermissionsForRoles(roles));
    }

    protected async Task<T> AssertException<T>(
        Func<Task> testCode,
        string? messageContains = null)
        where T : Exception
    {
        var ex = await Assert.ThrowsAsync<T>(testCode);
        if (messageContains != null)
        {
            ex.Message.Should().Contain(messageContains);
        }

        return ex;
    }

    private EventInfoTenant ToEventInfoTenant(Tenant tenant)
    {
        return new EventInfoTenant
        {
            Id = tenant.Id,
            Name = tenant.Name,
        };
    }

    private EventInfoUser ToEventInfoUser(Lib.Iam.Models.User user)
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
