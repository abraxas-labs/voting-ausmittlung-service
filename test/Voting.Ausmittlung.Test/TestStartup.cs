// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Messaging;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Services.Export;
using Voting.Ausmittlung.Core.Services.Validation.Utils;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Report.Services;
using Voting.Ausmittlung.Test.MockedData.Mapping;
using Voting.Ausmittlung.Test.Mocks;
using Voting.Lib.Ech;
using Voting.Lib.Messaging;
using Voting.Lib.Testing.Mocks;
using DokConnectorMock = Voting.Ausmittlung.Test.Mocks.DokConnectorMock;

namespace Voting.Ausmittlung.Test;

public class TestStartup : Startup
{
    public TestStartup(IConfiguration configuration)
        : base(configuration)
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services
            .AddMock<IPdfService, PdfServiceMock>()
            .AddMock<IValidationResultsEnsurerUtils, ValidationResultsEnsurerUtilsMock>()
            .AddMock<IDokConnector, DokConnectorMock>()
            .AddMock<IEchMessageIdProvider, MockEchMessageIdProvider>()
            .AddMock<IActionIdComparer, ActionIdComparerMock>()
            .AddMockedClock()
            .AddVotingLibEventingMocks()
            .AddVotingLibIamMocks()
            .AddVotingLibCryptographyMocks()
            .AddVotingLibPkcs11Mock()
            .RemoveHostedServices()
            .AddSingleton<TestMapper>();
    }

    protected override void ConfigureAuthentication(AuthenticationBuilder builder)
        => builder.AddMockedSecureConnectScheme();

    protected override void AddMessaging(IServiceCollection services)
    {
        services.AddVotingLibMessagingMocks(cfg =>
        {
            cfg.AddConsumerAndConsumerTestHarness<MessageConsumer<ResultStateChanged>>();
            cfg.AddConsumerAndConsumerTestHarness<MajorityElectionBundleChangedMessageConsumer>();
            cfg.AddConsumerAndConsumerTestHarness<ProportionalElectionBundleChangedMessageConsumer>();
            cfg.AddConsumerAndConsumerTestHarness<VoteBundleChangedMessageConsumer>();
        });
    }
}
