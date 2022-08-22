// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class ResultImportModelBuilder : IEntityTypeConfiguration<ResultImport>
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
    }
}
