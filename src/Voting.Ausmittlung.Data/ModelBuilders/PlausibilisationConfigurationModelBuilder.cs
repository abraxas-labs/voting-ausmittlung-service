// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class PlausibilisationConfigurationModelBuilder : IEntityTypeConfiguration<PlausibilisationConfiguration>,
        IEntityTypeConfiguration<ComparisonCountOfVotersConfiguration>,
        IEntityTypeConfiguration<ComparisonVoterParticipationConfiguration>,
        IEntityTypeConfiguration<ComparisonVotingChannelConfiguration>
{
    public void Configure(EntityTypeBuilder<PlausibilisationConfiguration> builder)
    {
        builder
            .HasOne(x => x.DomainOfInfluence)
            .WithOne(x => x.PlausibilisationConfiguration!)
            .HasForeignKey<PlausibilisationConfiguration>(x => x.DomainOfInfluenceId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<ComparisonCountOfVotersConfiguration> builder)
    {
        builder
            .HasOne(x => x.PlausibilisationConfiguration)
            .WithMany(x => x.ComparisonCountOfVotersConfigurations)
            .IsRequired();

        builder
            .HasIndex(x => new { x.PlausibilisationConfigurationId, x.Category })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ComparisonVoterParticipationConfiguration> builder)
    {
        builder
            .HasOne(x => x.PlausibilisationConfiguration)
            .WithMany(x => x.ComparisonVoterParticipationConfigurations)
            .IsRequired();

        builder
            .HasIndex(x => new { x.PlausibilisationConfigurationId, x.MainLevel, x.ComparisonLevel })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ComparisonVotingChannelConfiguration> builder)
    {
        builder
            .HasOne(x => x.PlausibilisationConfiguration)
            .WithMany(x => x.ComparisonVotingChannelConfigurations)
            .IsRequired();

        builder
            .HasIndex(x => new { x.PlausibilisationConfigurationId, x.VotingChannel })
            .IsUnique();
    }
}
