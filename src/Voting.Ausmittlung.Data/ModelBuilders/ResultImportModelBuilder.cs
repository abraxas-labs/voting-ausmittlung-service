// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class ResultImportModelBuilder : IEntityTypeConfiguration<ResultImport>, IEntityTypeConfiguration<ResultImportCountingCircle>
{
    public void Configure(EntityTypeBuilder<ResultImport> builder)
    {
        builder
            .HasOne(x => x.Contest!)
            .WithMany(x => x.ResultImports)
            .HasForeignKey(x => x.ContestId)
            .IsRequired();

        builder.OwnsOne(x => x.StartedBy);
        builder.Navigation(x => x.StartedBy).IsRequired();

        builder
            .Property(x => x.Started)
            .HasUtcConversion();
        builder
            .HasMany(x => x.IgnoredCountingCircles)
            .WithOne(x => x.ResultImport)
            .HasForeignKey(x => x.ResultImportId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<ResultImportCountingCircle> builder)
    {
        builder
            .HasOne(x => x.ResultImport)
            .WithMany(x => x.ImportedCountingCircles)
            .HasForeignKey(x => x.ResultImportId)
            .IsRequired();

        builder
            .HasOne(x => x.CountingCircle)
            .WithMany()
            .HasForeignKey(x => x.CountingCircleId)
            .IsRequired();
    }
}
