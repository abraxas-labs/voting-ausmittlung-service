// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.TemporaryData.Models;

namespace Voting.Ausmittlung.TemporaryData.ModelBuilders;

public class SecondFactorTransactionModelBuilder : IEntityTypeConfiguration<SecondFactorTransaction>
{
    public void Configure(EntityTypeBuilder<SecondFactorTransaction> builder)
    {
        builder
            .Property(x => x.LastUpdatedAt)
            .HasUtcConversion();

        builder
            .Property(x => x.CreatedAt)
            .HasUtcConversion();

        builder
            .Property(x => x.ExpiredAt)
            .HasUtcConversion();

        builder.HasIndex(x => x.ActionId)
            .IsUnique();
    }
}
