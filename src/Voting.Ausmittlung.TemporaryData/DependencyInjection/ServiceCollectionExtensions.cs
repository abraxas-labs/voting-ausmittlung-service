// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.TemporaryData.Configuration;
using Voting.Ausmittlung.TemporaryData.Repositories;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTemporaryData(
        this IServiceCollection services,
        TemporaryDataConfig config,
        Action<DbContextOptionsBuilder> optionsBuilder)
    {
        services.AddDbContext<TemporaryDataContext>(db =>
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
            .AddScoped<SecondFactorTransactionRepo>()
            .AddVotingLibDatabase<TemporaryDataContext>();
    }
}
