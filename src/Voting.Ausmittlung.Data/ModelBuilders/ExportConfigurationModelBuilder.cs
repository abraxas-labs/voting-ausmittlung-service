// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class ExportConfigurationModelBuilder :
    IEntityTypeConfiguration<ResultExportConfiguration>,
    IEntityTypeConfiguration<ResultExportConfigurationPoliticalBusiness>,
    IEntityTypeConfiguration<ExportConfiguration>
{
    public void Configure(EntityTypeBuilder<ResultExportConfiguration> builder)
    {
        builder
            .HasOne(x => x.Contest!)
            .WithMany(x => x.ResultExportConfigurations)
            .HasForeignKey(x => x.ContestId)
            .IsRequired();

        builder
            .HasMany(x => x.PoliticalBusinesses)
            .WithOne(x => x.ResultExportConfiguration!)
            .HasForeignKey(x => x.ResultExportConfigurationId)
            .IsRequired();

        builder
            .HasOne(x => x.DomainOfInfluence)
            .WithMany(x => x.ResultExportConfigurations)
            .HasForeignKey(x => x.DomainOfInfluenceId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<ResultExportConfigurationPoliticalBusiness> builder)
    {
        builder
            .HasIndex(x => new { x.PoliticalBusinessId, x.ResultExportConfigurationId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ExportConfiguration> builder)
    {
        builder
            .HasOne(x => x.DomainOfInfluence)
            .WithMany(x => x.ExportConfigurations)
            .HasForeignKey(x => x.DomainOfInfluenceId)
            .IsRequired();
    }
}
