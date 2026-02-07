// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Configuration;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Interceptors;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddData(
        this IServiceCollection services,
        DataConfig config,
        Action<DbContextOptionsBuilder> optionsBuilder)
    {
        services.AddDbContext<DataContext>((serviceProvider, db) =>
        {
            if (config.EnableDetailedErrors)
            {
                db.EnableDetailedErrors();
            }

            if (config.EnableSensitiveDataLogging)
            {
                db.EnableSensitiveDataLogging();
            }

            if (config.EnableMonitoring)
            {
                db.AddInterceptors(serviceProvider.GetRequiredService<DatabaseQueryMonitoringInterceptor>());
            }

            db.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            optionsBuilder(db);
        });

        if (config.EnableMonitoring)
        {
            services.AddDataMonitoring(config.Monitoring);
        }

        return services
            .AddScoped<ResultImportRepo>()
            .AddScoped<BallotRepo>()
            .AddScoped<BallotTranslationRepo>()
            .AddScoped<BallotQuestionTranslationRepo>()
            .AddScoped<TieBreakQuestionTranslationRepo>()
            .AddScoped<DomainOfInfluenceCountingCircleRepo>()
            .AddScoped<DomainOfInfluencePermissionRepo>()
            .AddScoped<VoteRepo>()
            .AddScoped<VoteTranslationRepo>()
            .AddScoped<MajorityElectionRepo>()
            .AddScoped<MajorityElectionTranslationRepo>()
            .AddScoped<ProportionalElectionRepo>()
            .AddScoped<ProportionalElectionTranslationRepo>()
            .AddScoped<VoteResultRepo>()
            .AddScoped<VoteEndResultRepo>()
            .AddScoped<ProportionalElectionResultRepo>()
            .AddScoped<ProportionalElectionResultBallotRepo>()
            .AddScoped<ProportionalElectionResultBallotCandidateRepo>()
            .AddScoped<ProportionalElectionEndResultRepo>()
            .AddScoped<ProportionalElectionListRepo>()
            .AddScoped<ProportionalElectionListTranslationRepo>()
            .AddScoped<ProportionalElectionListEndResultRepo>()
            .AddScoped<ProportionalElectionListUnionTranslationRepo>()
            .AddScoped<ProportionalElectionCandidateRepo>()
            .AddScoped<ProportionalElectionCandidateTranslationRepo>()
            .AddScoped<MajorityElectionResultRepo>()
            .AddScoped<MajorityElectionCandidateRepo>()
            .AddScoped<MajorityElectionCandidateTranslationRepo>()
            .AddScoped<MajorityElectionEndResultRepo>()
            .AddScoped<SecondaryMajorityElectionRepo>()
            .AddScoped<SecondaryMajorityElectionTranslationRepo>()
            .AddScoped<SecondaryMajorityElectionCandidateRepo>()
            .AddScoped<SecondaryMajorityElectionCandidateTranslationRepo>()
            .AddScoped<ProportionalElectionListUnionEntryRepo>()
            .AddScoped<ContestRepo>()
            .AddScoped<ContestTranslationRepo>()
            .AddScoped<DomainOfInfluenceRepo>()
            .AddScoped<CountingCircleRepo>()
            .AddScoped<CountingCircleContactPersonRepo>()
            .AddScoped<ProportionalElectionUnionEntryRepo>()
            .AddScoped<MajorityElectionUnionEntryRepo>()
            .AddScoped<ProportionalElectionUnionListRepo>()
            .AddScoped<ProportionalElectionUnionListTranslationRepo>()
            .AddScoped<SimplePoliticalBusinessTranslationRepo>()
            .AddScoped<SimplePoliticalBusinessRepo>()
            .AddScoped<SimpleCountingCircleResultRepo>()
            .AddScoped<CountOfVotersInformationSubTotalRepo>()
            .AddScoped<CantonSettingsRepo>()
            .AddScoped<ResultExportConfigurationRepo>()
            .AddScoped<ContestCountingCircleDetailsRepo>()
            .AddScoped<DomainOfInfluencePartyRepo>()
            .AddScoped<ProportionalElectionUnionRepo>()
            .AddScoped<DoubleProportionalResultRepo>()
            .AddVotingLibDatabase<DataContext>();
    }
}
