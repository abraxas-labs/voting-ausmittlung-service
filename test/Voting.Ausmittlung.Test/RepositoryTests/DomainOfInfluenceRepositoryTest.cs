// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using FluentAssertions;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.RepositoryTests;

public class DomainOfInfluenceRepositoryTest : BaseIntegrationTest
{
    private readonly DomainOfInfluenceRepo _doiRepo;

    public DomainOfInfluenceRepositoryTest(TestApplicationFactory factory)
        : base(factory)
    {
        _doiRepo = GetService<DomainOfInfluenceRepo>();
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task GetLowestCommonAncestorWithSiblingsShouldWork()
    {
        var doi = await _doiRepo.GetLowestCommonAncestorOrSelf(new[]
        {
            DomainOfInfluenceMockedData.GuidGossau,
            DomainOfInfluenceMockedData.GuidUzwil,
            DomainOfInfluenceMockedData.GuidStGallenStadt,
        });

        doi.Id.Should().Be(DomainOfInfluenceMockedData.GuidStGallen);
    }

    [Fact]
    public async Task GetLowestCommonAncestorSelfShouldWork()
    {
        var doi = await _doiRepo.GetLowestCommonAncestorOrSelf(new[]
        {
            DomainOfInfluenceMockedData.GuidGossau,
            DomainOfInfluenceMockedData.GuidStGallen,
        });

        doi.Id.Should().Be(DomainOfInfluenceMockedData.GuidStGallen);
    }

    [Fact]
    public async Task GetLowestCommonAncestorRootAndSelfShouldWork()
    {
        var doi = await _doiRepo.GetLowestCommonAncestorOrSelf(new[]
        {
            DomainOfInfluenceMockedData.GuidBund,
        });

        doi.Id.Should().Be(DomainOfInfluenceMockedData.GuidBund);
    }
}
