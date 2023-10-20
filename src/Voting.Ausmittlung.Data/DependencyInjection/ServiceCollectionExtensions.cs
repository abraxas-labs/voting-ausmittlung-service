// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Configuration;
using Voting.Ausmittlung.Data.Repositories;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddData(
        this IServiceCollection services,
        DataConfig config,
        Action<DbContextOptionsBuilder> optionsBuilder)
    {
        services.AddDbContext<DataContext>(db =>
        {
            if (config.EnableDetailedErrors)
            {
                db.EnableDetailedErrors();
            }

            if (config.EnableSensitiveDataLogging)
            {
                db.EnableSensitiveDataLogging();
            }

            db.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            optionsBuilder(db);
        });

        return services
            .AddScoped<ResultImportRepo>()
            .AddScoped<BallotRepo>()
            .AddScoped<BallotTranslationRepo>()
            .AddScoped<BallotQuestionRepo>()
            .AddScoped<BallotQuestionTranslationRepo>()
            .AddScoped<TieBreakQuestionRepo>()
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
            .AddVotingLibDatabase<DataContext>();
    }
}
