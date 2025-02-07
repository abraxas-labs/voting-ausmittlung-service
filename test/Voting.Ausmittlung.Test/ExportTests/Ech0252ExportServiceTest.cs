// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Services.Export;
using Voting.Ausmittlung.Core.Services.Export.Models;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Authorization;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests;

public class Ech0252ExportServiceTest : BaseIntegrationTest
{
    public Ech0252ExportServiceTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task TestShouldReturnContests()
    {
        var filter = new Ech0252FilterModel
        {
            ContestDateFrom = new DateTime(2020, 1, 1),
            ContestDateTo = new DateTime(2021, 1, 1),
        };

        var result = await LoadContests(filter);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldReturnContestOfExactDate()
    {
        var filter = new Ech0252FilterModel
        {
            ContestDateFrom = new DateTime(2020, 08, 31),
        };

        var result = await LoadContests(filter);
        result.Count().Should().Be(1);
        result[0].Date.Should().Be(new DateTime(2020, 08, 31));
        result[0].Results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldWorkWithPbIdsFilter()
    {
        var voteId1 = Guid.Parse(VoteMockedData.IdStGallenVoteInContestStGallen);
        var voteId2 = Guid.Parse(VoteMockedData.IdBundVoteInContestStGallen);

        var filter = new Ech0252FilterModel
        {
            ContestDateFrom = new DateTime(2020, 1, 1),
            ContestDateTo = new DateTime(2021, 1, 1),
            PoliticalBusinessIds = new()
            {
                voteId1,
                voteId2,
            },
        };

        var result = await LoadContests(filter);
        result.Should().HaveCount(1);
        result[0].Date.Should().Be(new DateTime(2020, 08, 31));
        result[0].Results.Should().NotBeEmpty();
        result[0].Results.All(r => r.PoliticalBusinessId == voteId1 || r.PoliticalBusinessId == voteId2);
    }

    [Fact]
    public async Task ShouldWorkWithCcResultStateFilterAndElections()
    {
        var electionId = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen);

        var filter = new Ech0252FilterModel
        {
            ContestDateFrom = new DateTime(2020, 8, 31),
            ContestDateTo = new DateTime(2020, 8, 31),
            CountingCircleResultStates = new() { CountingCircleResultState.Plausibilised, CountingCircleResultState.AuditedTentatively },
        };

        var result = await LoadContests(filter);

        // Should be included, CC state filter will be applied later on
        result[0].Results.Any(r => r.PoliticalBusinessId == electionId).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldWorkWithPbTypesFilter()
    {
        var filter = new Ech0252FilterModel
        {
            ContestDateFrom = new DateTime(2020, 1, 1),
            ContestDateTo = new DateTime(2021, 1, 1),
            PoliticalBusinessTypes = new() { PoliticalBusinessType.Vote },
        };

        var result = await LoadContests(filter);
        result.Should().HaveCount(3);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task NoDateShouldThrow()
    {
        var filterRequest = new Ech0252FilterRequest();
        await AssertException<ValidationException>(() => BuildAndValidateFilter(filterRequest), "Contest date or date range is required");
    }

    [Fact]
    public async Task DateRangeToLargeShouldThrow()
    {
        var filterRequest = new Ech0252FilterRequest()
        {
            PollingDateFrom = new DateTime(2011, 1, 1),
            PollingDateTo = new DateTime(2022, 1, 1),
        };
        await AssertException<ValidationException>(() => BuildAndValidateFilter(filterRequest), "Date range can maximaly be");

        filterRequest = new Ech0252FilterRequest()
        {
            PollingSinceDays = 3651,
        };
        await AssertException<ValidationException>(() => BuildAndValidateFilter(filterRequest), "Since days can maximaly be");
    }

    [Fact]
    public async Task GreaterFromThanToDateShouldThrow()
    {
        var filterRequest = new Ech0252FilterRequest
        {
            PollingDateFrom = DateTime.Parse("2020-03-03"),
            PollingDateTo = DateTime.Parse("2020-03-02"),
        };
        await AssertException<ValidationException>(() => BuildAndValidateFilter(filterRequest), "From date must be smaller or equal to date");
    }

    [Theory]
    [InlineData("2023-02-01", "2023-02-05", null, null, "2023-02-01", "2023-02-05")]
    [InlineData(null, null, "2023-03-04", null, "2023-03-04", null)]
    [InlineData(null, null, null, 2, "2020-01-08", "2020-01-10")]
    public void TestValidDateFilters(string? from, string? to, string? date, int? sinceDays, string? expectedFrom, string? expectedTo)
    {
        var service = GetService<Ech0252ExportService>();
        var contestDateFrom = ParseNullableDateTime(from);
        var contestDateTo = ParseNullableDateTime(to);
        var contestDate = ParseNullableDateTime(date);
        var expectedDateFrom = ParseNullableDateTime(expectedFrom);
        var expectedDateTo = ParseNullableDateTime(expectedTo);

        var filterRequest = new Ech0252FilterRequest
        {
            PollingDate = contestDate,
            PollingDateFrom = contestDateFrom,
            PollingDateTo = contestDateTo,
            PollingSinceDays = sinceDays,
        };

        var result = service.BuildAndValidateFilter(filterRequest);
        result.ContestDateFrom.Should().Be(expectedDateFrom);
        result.ContestDateTo.Should().Be(expectedDateTo);
    }

    [Theory]
    [InlineData("2023-02-01", "2023-02-05", "2023-02-04", null)]
    [InlineData("2023-02-01", "2023-02-05", null, 2)]
    [InlineData(null, null, "2020-01-10", 2)]
    public Task TestInvalidDateFilters(string? from, string? to, string? date, int? sinceDays)
    {
        var service = GetService<Ech0252ExportService>();
        var contestDateFrom = ParseNullableDateTime(from);
        var contestDateTo = ParseNullableDateTime(to);
        var contestDate = ParseNullableDateTime(date);

        var filterRequest = new Ech0252FilterRequest
        {
            PollingDate = contestDate,
            PollingDateFrom = contestDateFrom,
            PollingDateTo = contestDateTo,
            PollingSinceDays = sinceDays,
        };

        return AssertException<ValidationException>(
            () => Task.FromResult(service.BuildAndValidateFilter(filterRequest)),
            "Only one date filter is allowed and required. Choose one between date, date range and since days.");
    }

    private async Task<List<SimpleContest>> LoadContests(Ech0252FilterModel filter)
    {
        return await RunExportService(async service =>
        {
            var contests = await (await service.LoadContests(filter)).ToListAsync();

            return contests
                .Where(c => !service.PrepareContestDataAndCheckIsEmpty(c))
                .Select(c =>
                {
                    var simpleContest = new SimpleContest
                    {
                        Id = c.Id,
                        Date = c.Date,
                    };

                    var ccResults = c.Votes.SelectMany(v => v.Results)
                        .OfType<CountingCircleResult>()
                        .Concat(c.ProportionalElections.SelectMany(p => p.Results))
                        .Concat(c.MajorityElections.SelectMany(m => m.Results))
                        .OrderBy(x => x.Id)
                        .ToList();

                    simpleContest.Results = ccResults.ConvertAll(ccResult => new SimpleCountingCircleResult
                    {
                        PoliticalBusinessId = ccResult.PoliticalBusinessId,
                        CountingCircleId = ccResult.CountingCircle.BasisCountingCircleId,
                        PoliticalBusinessType = ccResult.PoliticalBusiness.BusinessType,
                    });
                    return simpleContest;
                })
                .ToList();
        });
    }

    private Task<Ech0252FilterModel> BuildAndValidateFilter(Ech0252FilterRequest request)
    {
        return RunExportService(service => Task.FromResult(service.BuildAndValidateFilter(request)));
    }

    private DateTime? ParseNullableDateTime(string? s) => s == null ? null : DateTime.Parse(s);

    private async Task<T> RunExportService<T>(Func<Ech0252ExportService, Task<T>> func)
    {
        return await RunScoped<IServiceProvider, T>(async sp =>
        {
            var permissionProvider = sp.GetRequiredService<IPermissionProvider>();
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", "test", SecureConnectTestDefaults.MockedTenantStGallen.Id, null, permissionProvider.GetPermissionsForRoles(new[] { RolesMockedData.ReportExporterApi }));

            var service = sp.GetRequiredService<Ech0252ExportService>();
            return await func(service);
        });
    }

    private class SimpleContest
    {
        public Guid Id { get; set; }

        public DateTime Date { get; set; }

        public List<SimpleCountingCircleResult> Results { get; set; } = new();
    }

    private class SimpleCountingCircleResult
    {
        public Guid CountingCircleId { get; set; }

        public Guid PoliticalBusinessId { get; set; }

        public PoliticalBusinessType PoliticalBusinessType { get; set; }
    }
}
