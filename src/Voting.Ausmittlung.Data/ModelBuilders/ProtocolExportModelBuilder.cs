// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class ProtocolExportModelBuilder : IEntityTypeConfiguration<ProtocolExport>
{
    public void Configure(EntityTypeBuilder<ProtocolExport> builder)
    {
        builder
            .HasOne(x => x.Contest!)
            .WithMany()
            .HasForeignKey(x => x.ContestId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .Property(x => x.Started)
            .HasUtcConversion();
    }
}
