// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class ProportionalElectionModelBuilder :
    IEntityTypeConfiguration<ProportionalElection>,
    IEntityTypeConfiguration<ProportionalElectionTranslation>,
    IEntityTypeConfiguration<ProportionalElectionList>,
    IEntityTypeConfiguration<ProportionalElectionListTranslation>,
    IEntityTypeConfiguration<ProportionalElectionListUnion>,
    IEntityTypeConfiguration<ProportionalElectionListUnionTranslation>,
    IEntityTypeConfiguration<ProportionalElectionListUnionEntry>,
    IEntityTypeConfiguration<ProportionalElectionCandidate>,
    IEntityTypeConfiguration<ProportionalElectionCandidateTranslation>,
    IEntityTypeConfiguration<ProportionalElectionUnion>,
    IEntityTypeConfiguration<ProportionalElectionUnionEntry>,
    IEntityTypeConfiguration<ProportionalElectionUnionList>,
    IEntityTypeConfiguration<ProportionalElectionUnionListTranslation>,
    IEntityTypeConfiguration<ProportionalElectionUnionListEntry>
{
    public void Configure(EntityTypeBuilder<ProportionalElection> builder)
    {
        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.ProportionalElection!)
            .HasForeignKey(x => x.ProportionalElectionId);

        builder
            .HasOne(v => v.DomainOfInfluence)
            .WithMany(di => di.ProportionalElections)
            .HasForeignKey(v => v.DomainOfInfluenceId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(v => v.Contest)
            .WithMany(c => c.ProportionalElections)
            .HasForeignKey(v => v.ContestId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasMany(p => p.ProportionalElectionLists)
            .WithOne(l => l.ProportionalElection)
            .HasForeignKey(l => l.ProportionalElectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasMany(p => p.ProportionalElectionListUnions)
            .WithOne(lu => lu.ProportionalElection)
            .HasForeignKey(lu => lu.ProportionalElectionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(b => new { b.ProportionalElectionId, b.Language })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionList> builder)
    {
        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.ProportionalElectionList!)
            .HasForeignKey(x => x.ProportionalElectionListId);

        builder
            .HasMany(p => p.ProportionalElectionCandidates)
            .WithOne(l => l.ProportionalElectionList)
            .HasForeignKey(l => l.ProportionalElectionListId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionListTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(b => new { b.ProportionalElectionListId, b.Language })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionListUnion> builder)
    {
        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.ProportionalElectionListUnion!)
            .HasForeignKey(x => x.ProportionalElectionListUnionId);

        builder
            .HasOne(lu => lu.ProportionalElectionRootListUnion)
            .WithMany(lu => lu!.ProportionalElectionSubListUnions)
            .HasForeignKey(lu => lu.ProportionalElectionRootListUnionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(lu => lu.ProportionalElectionMainList)
            .WithMany(l => l!.ProportionalElectionMainListUnions)
            .HasForeignKey(lu => lu.ProportionalElectionMainListId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionListUnionTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(b => new { b.ProportionalElectionListUnionId, b.Language })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionListUnionEntry> builder)
    {
        builder
            .HasKey(e => new { e.ProportionalElectionListId, e.ProportionalElectionListUnionId });

        builder
            .HasOne(e => e.ProportionalElectionList)
            .WithMany(l => l.ProportionalElectionListUnionEntries)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(e => e.ProportionalElectionListUnion)
            .WithMany(lu => lu.ProportionalElectionListUnionEntries)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionCandidate> builder)
    {
        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.ProportionalElectionCandidate!)
            .HasForeignKey(x => x.ProportionalElectionCandidateId);

        builder
            .Property(d => d.DateOfBirth)
            .HasDateType()
            .HasUtcConversion()
            .IsRequired();

        builder
            .HasOne(x => x.Party)
            .WithMany(x => x.ProportionalElectionCandidates)
            .OnDelete(DeleteBehavior.SetNull); // required for contest delete.
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionCandidateTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(b => new { b.ProportionalElectionCandidateId, b.Language })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionUnion> builder)
    {
        builder
            .HasOne(pu => pu.Contest)
            .WithMany(c => c.ProportionalElectionUnions)
            .HasForeignKey(pu => pu.ContestId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionUnionEntry> builder)
    {
        builder
            .HasOne(pe => pe.ProportionalElectionUnion)
            .WithMany(pu => pu.ProportionalElectionUnionEntries)
            .HasForeignKey(pe => pe.ProportionalElectionUnionId)
            .IsRequired();

        builder
            .HasOne(pe => pe.ProportionalElection)
            .WithMany(pu => pu.ProportionalElectionUnionEntries)
            .HasForeignKey(pe => pe.ProportionalElectionId)
            .IsRequired();

        builder
            .HasIndex(pe => new { pe.ProportionalElectionId, pe.ProportionalElectionUnionId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionUnionListEntry> builder)
    {
        builder
            .HasOne(ple => ple.ProportionalElectionUnionList)
            .WithMany(pul => pul.ProportionalElectionUnionListEntries)
            .HasForeignKey(ple => ple.ProportionalElectionUnionListId)
            .IsRequired();

        builder
            .HasOne(ple => ple.ProportionalElectionList)
            .WithMany(pl => pl.ProportionalElectionUnionListEntries)
            .HasForeignKey(ple => ple.ProportionalElectionListId)
            .IsRequired();

        builder
            .HasIndex(ple => new { ple.ProportionalElectionListId, ple.ProportionalElectionUnionListId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionUnionList> builder)
    {
        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.ProportionalElectionUnionList!)
            .HasForeignKey(x => x.ProportionalElectionUnionListId);

        builder
            .HasOne(pul => pul.ProportionalElectionUnion)
            .WithMany(pu => pu.ProportionalElectionUnionLists)
            .HasForeignKey(pul => pul.ProportionalElectionUnionId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionUnionListTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(b => new { b.ProportionalElectionUnionListId, b.Language })
            .IsUnique();
    }
}
