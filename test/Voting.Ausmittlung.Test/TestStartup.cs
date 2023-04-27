// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Messaging;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Services.Validation.Utils;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Report.Services;
using Voting.Ausmittlung.Test.MockedData.Mapping;
using Voting.Ausmittlung.Test.Mocks;
using Voting.Lib.Ech;
using Voting.Lib.Messaging;
using Voting.Lib.Testing.Mocks;

namespace Voting.Ausmittlung.Test;

public class TestStartup : Startup
{
    public TestStartup(IConfiguration configuration, IWebHostEnvironment environment)
        : base(configuration, environment)
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services
            .AddMock<IPdfService, PdfServiceMock>()
            .AddMock<IValidationResultsEnsurerUtils, ValidationResultsEnsurerUtilsMock>()
            .AddMock<IEchMessageIdProvider, MockEchMessageIdProvider>()
            .AddMock<IActionIdComparer, ActionIdComparerMock>()
            .AddMockedClock()
            .AddVotingLibEventingMocks()
            .AddVotingLibIamMocks()
            .AddDokConnectorMock()
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
            cfg.AddConsumerAndConsumerTestHarness<MessageConsumer<ProtocolExportStateChanged>>();
            cfg.AddConsumerAndConsumerTestHarness<MajorityElectionBundleChangedMessageConsumer>();
            cfg.AddConsumerAndConsumerTestHarness<ProportionalElectionBundleChangedMessageConsumer>();
            cfg.AddConsumerAndConsumerTestHarness<VoteBundleChangedMessageConsumer>();
        });
    }
}
