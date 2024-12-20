// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Jobs;
using Voting.Ausmittlung.Core.Messaging;
using Voting.Ausmittlung.Core.Services;
using Voting.Ausmittlung.Core.Services.Export;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Core.Services.Validation;
using Voting.Ausmittlung.Core.Services.Validation.Utils;
using Voting.Ausmittlung.Core.Services.Write;
using Voting.Ausmittlung.Core.Services.Write.Import;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Core.Validation;
using Voting.Basis.Core.EventSignature;
using Voting.Lib.DokConnector.Service;
using Voting.Lib.Eventing;
using Voting.Lib.Eventing.DependencyInjection;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Authorization;
using Voting.Lib.Scheduler;
using VotingValidators = Voting.Ausmittlung.Core.Services.Validation.Validators;

namespace Microsoft.Extensions.DependencyInjection;

internal static class PublisherServiceCollectionExtensions
{
    internal static IServiceCollection AddPublisherServices(this IServiceCollection services, AppConfig config)
    {
        if (!config.PublisherModeEnabled)
        {
            return services;
        }

        return services
            .AddScoped<PermissionService>()
            .AddSingleton<IPermissionProvider, PermissionProvider>()
            .AddScoped<LanguageService>()
            .AddWriterServices(config.Publisher)
            .AddReaderServices();
    }

    internal static IEventingServiceCollection AddPublisher(this IEventingServiceCollection services, AppConfig config)
    {
        if (config.PublisherModeEnabled)
        {
            services.AddPublishing<VoteResultAggregate>();

            if (config.EventSignature.Enabled)
            {
                services.AddTransientSubscription<VoteResultAggregate>(WellKnownStreams.All);
            }
        }

        return services;
    }

    private static IServiceCollection AddWriterServices(this IServiceCollection services, PublisherConfig config)
    {
        return services
            .AddValidation()
            .AddValidatorsFromAssemblyContaining<VotingCardResultDetailValidator>()
            .AddScoped<EventInfoProvider>()
            .AddDokConnector(config)
            .AddSingleton<IActionIdComparer, ActionIdComparer>()
            .AddScheduledJobs(config)
            .AddScoped<IAggregateRepositoryHandler, EventSignatureAggregateRepositoryHandler>()
            .AddScoped<ResultWriter>()
            .AddScoped<ContestCountingCircleDetailsWriter>()
            .AddScoped<ContestCountingCircleContactPersonWriter>()
            .AddScoped<ContestCountingCircleElectorateWriter>()
            .AddScoped<VoteResultWriter>()
            .AddScoped<VoteEndResultWriter>()
            .AddScoped<ProportionalElectionResultWriter>()
            .AddScoped<ProportionalElectionEndResultWriter>()
            .AddScoped<ProportionalElectionUnionEndResultWriter>()
            .AddScoped<ProportionalElectionResultBundleWriter>()
            .AddScoped<MajorityElectionResultWriter>()
            .AddScoped<MajorityElectionResultBundleWriter>()
            .AddScoped<MajorityElectionEndResultWriter>()
            .AddScoped<VoteResultBundleWriter>()
            .AddScoped<ResultImportWriter>()
            .AddScoped<MajorityElectionResultImportWriter>()
            .AddScoped<SecondaryMajorityElectionResultImportWriter>()
            .AddScoped<ProportionalElectionResultImportWriter>()
            .AddScoped<VoteResultImportWriter>()
            .AddScoped<ResultExportConfigurationWriter>()
            .AddScoped<SecondFactorTransactionWriter>()
            .AddScoped<EventSignatureWriter>()
            .AddScoped<DoubleProportionalResultWriter>();
    }

    private static IServiceCollection AddReaderServices(this IServiceCollection services)
    {
        return services
            .AddScoped<ContestService>()
            .AddScoped<ContestReader>()
            .AddScoped<ResultReader>()
            .AddScoped<ResultImportReader>()
            .AddScoped<ProportionalElectionResultReader>()
            .AddScoped<ProportionalElectionReader>()
            .AddScoped<VoteResultReader>()
            .AddScoped<VoteEndResultReader>()
            .AddScoped<ProportionalElectionEndResultReader>()
            .AddScoped<ProportionalElectionResultBundleReader>()
            .AddScoped<ProportionalElectionUnionEndResultReader>()
            .AddScoped<DoubleProportionalResultReader>()
            .AddScoped<MajorityElectionReader>()
            .AddScoped<MajorityElectionResultReader>()
            .AddScoped<MajorityElectionResultBundleReader>()
            .AddScoped<MajorityElectionEndResultReader>()
            .AddScoped<VoteResultBundleReader>()
            .AddScoped<BallotResultReader>()
            .AddScoped<ResultExportTemplateReader>()
            .AddScoped<ResultExportConfigurationReader>()
            .AddScoped<SimplePoliticalBusinessReader>()
            .AddScoped<SimpleCountingCircleResultReader>()
            .AddScoped<ResultExportService>()
            .AddScoped<ProtocolExportService>()
            .AddScoped<Ech0252ExportService>()
            .AddScoped<ExportRateLimitService>()
            .AddScoped<IExportProviderUploader, StandardProviderUploader>()
            .AddScoped<IExportProviderUploader, SeantisProviderUploader>()
            .AddScoped<PoliticalBusinessResultBundleBuilder>()
            .AddSingleton(typeof(LanguageAwareMessageConsumerHub<,>));
    }

    private static IServiceCollection AddScheduledJobs(this IServiceCollection services, PublisherConfig config)
    {
        return services
            .AddScheduledJob<ResultExportsJob>(config.AutomaticExports)
            .AddScheduledJob<CleanSecondFactorTransactionsJob>(config.CleanSecondFactorTransactionsJob)
            .AddScheduledJob<CleanExportLogEntriesJob>(config.CleanExportLogEntriesJob);
    }

    private static IServiceCollection AddValidation(this IServiceCollection services)
    {
        return services
            .AddScoped<IValidationResultsEnsurerUtils, ValidationResultsEnsurerUtils>()
            .AddScoped<ValidationResultsEnsurer>()
            .AddScoped<VoteResultValidationSummaryBuilder>()
            .AddScoped<ProportionalElectionResultValidationSummaryBuilder>()
            .AddScoped<MajorityElectionResultValidationSummaryBuilder>()
            .AddScoped<CountingCircleResultsValidationSummariesBuilder>()
            .AddValidators();
    }

    private static IServiceCollection AddValidators(this IServiceCollection services)
    {
        return services.Scan(scan =>
            scan.FromAssemblyOf<ValidationResultsEnsurer>()
                .AddClasses(classes => classes.AssignableTo(typeof(VotingValidators.IValidator<>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime());
    }

    private static IServiceCollection AddDokConnector(this IServiceCollection services, PublisherConfig config)
    {
        if (config.EnableDokConnectorMock)
        {
            return services.AddSingleton<IDokConnector, DokConnectorMock>();
        }

        services.AddEaiDokConnector(config.DokConnector)
            .AddSecureConnectServiceToken(PublisherConfig.SharedSecureConnectServiceAccountName);
        return services;
    }
}
