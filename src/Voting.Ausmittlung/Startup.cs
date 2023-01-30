// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Text.Json.Serialization;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.Registration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using Voting.Ausmittlung.Converters;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.Mapping;
using Voting.Ausmittlung.Core.Messaging;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Ech.DependencyInjection;
using Voting.Ausmittlung.Interceptors;
using Voting.Ausmittlung.Middlewares;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;
using Voting.Ausmittlung.Services;
using Voting.Ausmittlung.TemporaryData;
using Voting.Lib.Common.DependencyInjection;
using Voting.Lib.Grpc.DependencyInjection;
using Voting.Lib.Grpc.Interceptors;
using Voting.Lib.Messaging;
using Voting.Lib.Rest.Middleware;
using Voting.Lib.Rest.Swagger.DependencyInjection;
using ExceptionHandler = Voting.Ausmittlung.Middlewares.ExceptionHandler;
using ExceptionInterceptor = Voting.Ausmittlung.Interceptors.ExceptionInterceptor;

namespace Voting.Ausmittlung;

public class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly AppConfig _appConfig;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
        _appConfig = configuration.Get<AppConfig>();
    }

    public virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(_appConfig);
        services.AddCertificatePinning(_appConfig.CertificatePinning);
        ConfigureHealthChecks(services.AddHealthChecks());

        services.AddAutoMapper(typeof(Startup), typeof(ConverterProfile), typeof(PdfContestProfile));
        AddMessaging(services);

        services.AddCore(_appConfig);
        services.AddData(_appConfig.Database, ConfigureDatabase);
        services.AddVotingLibPrometheusAdapter(new() { Interval = _appConfig.PrometheusAdapterInterval });
        ConfigureAuthentication(services.AddVotingLibIam(new() { BaseUrl = _appConfig.SecureConnectApi }));

        if (_appConfig.PublisherModeEnabled)
        {
            AddPublisherServices(services);
        }
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        if (_appConfig.PublisherModeEnabled)
        {
            UsePublisher(app);
        }

        app.UseEndpoints(endpoints =>
        {
            // Metrics and health checks are always exposed, regardless of service mode
            endpoints.MapMetrics();
            endpoints.MapVotingHealthChecks(_appConfig.LowPriorityHealthCheckNames);

            if (_appConfig.PublisherModeEnabled)
            {
                MapEndpoints(endpoints);
            }
        });
    }

    protected virtual void ConfigureAuthentication(AuthenticationBuilder builder)
        => builder.AddSecureConnectScheme(options =>
        {
            options.Audience = _appConfig.SecureConnect.Audience;
            options.Authority = _appConfig.SecureConnect.Authority;
            options.FetchRoleToken = true;
            options.ServiceAccount = _appConfig.SecureConnect.ServiceAccount;
            options.ServiceAccountPassword = _appConfig.SecureConnect.ServiceAccountPassword;
            options.RoleTokenApps = _appConfig.SecureConnect.RoleTokenApps;
        });

    protected void ConfigureDatabase(DbContextOptionsBuilder db)
    {
        db.UseNpgsql(_appConfig.Database.ConnectionString);

#if DEBUG
        // The warning for the missing query split behavior should throw an exception.
        db.ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
#endif
    }

    protected void ConfigureTemporaryDatabase(DbContextOptionsBuilder db)
        => db.UseNpgsql(_appConfig.Publisher.TemporaryDatabase.ConnectionString);

    protected virtual void AddMessaging(IServiceCollection services)
        => services.AddVotingLibMessaging(_appConfig.RabbitMq, ConfigureMessagingBus);

    private void UsePublisher(IApplicationBuilder app)
    {
        if (_appConfig.Publisher.EnableGrpcWeb)
        {
            app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
            app.UseCorsFromConfig();
        }

        app.UseMiddleware<ExceptionHandler>();
        app.UseMiddleware<LanguageMiddleware>();
        app.UseHttpMetrics();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseMiddleware<IamLoggingHandler>();
        app.UseSerilogRequestLoggingWithTraceabilityModifiers();
        app.UseSwaggerGenerator();
    }

    private void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();
        endpoints.MapGrpcReflectionService();
        endpoints.MapGrpcService<ContestService>();
        endpoints.MapGrpcService<ResultService>();
        endpoints.MapGrpcService<ResultImportService>();
        endpoints.MapGrpcService<ContestCountingCircleDetailsService>();
        endpoints.MapGrpcService<ContestCountingCircleContactPersonService>();
        endpoints.MapGrpcService<VoteResultService>();
        endpoints.MapGrpcService<ProportionalElectionService>();
        endpoints.MapGrpcService<ProportionalElectionResultService>();
        endpoints.MapGrpcService<ProportionalElectionResultBundleService>();
        endpoints.MapGrpcService<MajorityElectionService>();
        endpoints.MapGrpcService<MajorityElectionResultService>();
        endpoints.MapGrpcService<MajorityElectionResultBundleService>();
        endpoints.MapGrpcService<VoteResultBundleService>();
        endpoints.MapGrpcService<ExportService>();
    }

    private void ConfigureMessagingBus(IServiceCollectionBusConfigurator cfg)
    {
        if (!_appConfig.PublisherModeEnabled)
        {
            return;
        }

        cfg.AddConsumer<MessageConsumer<ResultStateChanged>>().Endpoint(ConfigureMessagingConsumerEndpoint);
        cfg.AddConsumer<MajorityElectionBundleChangedMessageConsumer>().Endpoint(ConfigureMessagingConsumerEndpoint);
        cfg.AddConsumer<ProportionalElectionBundleChangedMessageConsumer>().Endpoint(ConfigureMessagingConsumerEndpoint);
        cfg.AddConsumer<VoteBundleChangedMessageConsumer>().Endpoint(ConfigureMessagingConsumerEndpoint);
    }

    private void ConfigureMessagingConsumerEndpoint(IConsumerEndpointRegistrationConfigurator config)
    {
        config.InstanceId = Environment.MachineName;
        config.Temporary = true;
    }

    private void ConfigureHealthChecks(IHealthChecksBuilder checks)
    {
        checks
            .AddDbContextCheck<DataContext>()
            .AddEventStore()
            .AddPkcs11HealthCheck()
            .ForwardToPrometheus();

        // Temporary data (eg. information about multi factor authentication) is only used in the Publisher configuration
        if (_appConfig.PublisherModeEnabled)
        {
            checks.AddDbContextCheck<TemporaryDataContext>();

            if (_appConfig.EventSignature.Enabled)
            {
                checks.AddEventStoreTransientSubscriptionCatchUp();
            }
        }
    }

    private IServiceCollection AddPublisherServices(IServiceCollection services)
    {
        services.AddSingleton(_appConfig.Publisher);

        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new OptionalGuidConverter());
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddGrpc(o =>
        {
            o.EnableDetailedErrors = _appConfig.Publisher.EnableDetailedErrors;
            o.Interceptors.Add<ExceptionInterceptor>();
            o.Interceptors.Add<LanguageInterceptor>();
            o.Interceptors.Add<RequestProtoValidatorInterceptor>();
        });

        services.AddGrpcRequestLoggerInterceptor(_environment);

        if (_appConfig.Publisher.EnableGrpcWeb)
        {
            services.AddCors(_configuration);
        }

        services.AddGrpcReflection();
        services.AddProtoValidators();

        services.AddReport(_appConfig.Publisher.Documatrix);
        services.AddEch(_appConfig.Publisher.Ech);
        services.AddTemporaryData(_appConfig.Publisher.TemporaryDatabase, ConfigureTemporaryDatabase);
        services.AddSwaggerGenerator(_configuration);

        services.AddSecureConnectServiceAccount(
            PublisherConfig.SharedSecureConnectServiceAccountName,
            _appConfig.Publisher.SharedSecureConnect);
        return services;
    }
}
