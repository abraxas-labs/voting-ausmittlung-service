// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.TemporaryData.Models;

namespace Voting.Ausmittlung.TemporaryData.ModelBuilders;

public class ExportLogEntryModelBuilder : IEntityTypeConfiguration<ExportLogEntry>
{
    public void Configure(EntityTypeBuilder<ExportLogEntry> builder)
    {
        builder
            .HasIndex(x => new { x.ExportKey, x.TenantId, x.Timestamp });

        builder
            .Property(x => x.Timestamp)
            .HasUtcConversion();
    }
}
