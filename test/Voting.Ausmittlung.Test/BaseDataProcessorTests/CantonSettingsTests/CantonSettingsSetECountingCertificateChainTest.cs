// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.MockedData.TestFiles;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.CantonSettingsTests;

public class CantonSettingsSetECountingCertificateChainTest : BaseDataProcessorTest
{
    public CantonSettingsSetECountingCertificateChainTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestProcessor()
    {
        await ContestMockedData.Seed(RunScoped);

        var id = Guid.Parse(CantonSettingsMockedData.IdStGallen);

        var cantonSettings = await RunOnDb(db => db.CantonSettings
            .AsSplitQuery()
            .Include(x => x.ECountingCertificateChains!)
            .ThenInclude(x => x.Infos!.OrderBy(x => x.Subject))
            .SingleAsync(x => x.Id == id));

        var existingCertChain = cantonSettings.ECountingCertificateChains.Single();

        await TestEventPublisher.Publish(new SetCantonSettingsECountingCertificateChain
        {
            EventInfo = GetMockedEventInfo(),
            Id = id.ToString(),
            RawPem = CertificateTestFiles.FourLevelChainRawPem,
        });

        cantonSettings = await RunOnDb(db => db.CantonSettings
            .AsSplitQuery()
            .Include(x => x.ECountingCertificateChains!)
            .ThenInclude(x => x.Infos!.OrderBy(x => x.Subject))
            .SingleAsync(x => x.Id == id));

        var certChains = cantonSettings.ECountingCertificateChains.ToList();
        certChains.Should().HaveCount(2);
        var newCertChain = certChains.Single(c => c.Id != existingCertChain.Id);

        foreach (var info in newCertChain.Infos!)
        {
            info.Id = Guid.Empty;
            info.CertificateChainId = Guid.Empty;
            info.CertificateChain = null!;
        }

        newCertChain.Id = Guid.Empty;
        newCertChain.CantonSettings = null!;
        newCertChain.MatchSnapshot();
    }
}
