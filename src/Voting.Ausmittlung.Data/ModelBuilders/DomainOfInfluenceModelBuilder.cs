// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class DomainOfInfluenceModelBuilder :
    IEntityTypeConfiguration<DomainOfInfluence>,
    IEntityTypeConfiguration<DomainOfInfluencePermissionEntry>,
    IEntityTypeConfiguration<DomainOfInfluenceCountingCircle>,
    IEntityTypeConfiguration<DomainOfInfluenceParty>,
    IEntityTypeConfiguration<DomainOfInfluencePartyTranslation>
{
    public void Configure(EntityTypeBuilder<DomainOfInfluence> builder)
    {
        builder
            .HasOne(di => di.Parent!)
            .WithMany(di => di!.Children)
            .HasForeignKey(di => di.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(di => di.SnapshotContest!)
            .WithMany()
            .HasForeignKey(di => di.SnapshotContestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsOne(di => di.ContactPerson);
        builder.Navigation(di => di.ContactPerson).IsRequired();

        builder.Ignore(di => di.IsSnapshot);
        builder.OwnsOne(doi => doi.CantonDefaults, b =>
        {
            b.OwnsMany(x => x.EnabledVotingCardChannels);
        });
        builder.Navigation(doi => doi.CantonDefaults).IsRequired();
    }

    public void Configure(EntityTypeBuilder<DomainOfInfluencePermissionEntry> builder)
    {
        builder
            .HasIndex(x => new { x.TenantId, x.BasisDomainOfInfluenceId, x.ContestId })
            .IsUnique();

        builder
            .HasOne(di => di.Contest!)
            .WithMany()
            .HasForeignKey(di => di.ContestId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<DomainOfInfluenceCountingCircle> builder)
    {
        builder
            .HasOne(dicc => dicc.CountingCircle)
            .WithMany(cc => cc!.DomainOfInfluences)
            .HasForeignKey(dicc => dicc.CountingCircleId)
            .IsRequired();

        builder
            .HasOne(dicc => dicc.DomainOfInfluence)
            .WithMany(di => di!.CountingCircles)
            .HasForeignKey(dicc => dicc.DomainOfInfluenceId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.CountingCircleId, x.DomainOfInfluenceId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<DomainOfInfluenceParty> builder)
    {
        builder
            .HasOne(x => x.DomainOfInfluence)
            .WithMany(x => x.Parties)
            .IsRequired();

        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.DomainOfInfluenceParty!)
            .HasForeignKey(x => x.DomainOfInfluencePartyId);

        builder
            .HasIndex(x => new { x.BaseDomainOfInfluencePartyId, x.SnapshotContestId })
            .IsUnique();

        builder
            .HasOne(x => x.SnapshotContest)
            .WithMany(x => x.DomainOfInfluenceParties)
            .HasForeignKey(x => x.SnapshotContestId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<DomainOfInfluencePartyTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(x => new { x.DomainOfInfluencePartyId, x.Language })
            .IsUnique();
    }
}
