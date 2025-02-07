// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class MajorityElectionModelBuilder :
    IEntityTypeConfiguration<MajorityElection>,
    IEntityTypeConfiguration<MajorityElectionTranslation>,
    IEntityTypeConfiguration<MajorityElectionCandidate>,
    IEntityTypeConfiguration<MajorityElectionCandidateTranslation>,
    IEntityTypeConfiguration<MajorityElectionUnion>,
    IEntityTypeConfiguration<MajorityElectionUnionEntry>,
    IEntityTypeConfiguration<SecondaryMajorityElectionCandidate>,
    IEntityTypeConfiguration<SecondaryMajorityElectionCandidateTranslation>,
    IEntityTypeConfiguration<SecondaryMajorityElection>,
    IEntityTypeConfiguration<SecondaryMajorityElectionTranslation>,
    IEntityTypeConfiguration<MajorityElectionBallotGroup>,
    IEntityTypeConfiguration<MajorityElectionBallotGroupEntry>,
    IEntityTypeConfiguration<MajorityElectionBallotGroupEntryCandidate>
{
    public void Configure(EntityTypeBuilder<MajorityElection> builder)
    {
        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.MajorityElection!)
            .HasForeignKey(x => x.MajorityElectionId);

        builder
            .HasOne(v => v.DomainOfInfluence)
            .WithMany(di => di.MajorityElections)
            .HasForeignKey(v => v.DomainOfInfluenceId)
            .IsRequired();

        builder
            .HasOne(v => v.Contest)
            .WithMany(c => c.MajorityElections)
            .HasForeignKey(v => v.ContestId)
            .IsRequired();

        builder
            .HasMany(p => p.MajorityElectionCandidates)
            .WithOne(l => l.MajorityElection)
            .HasForeignKey(l => l.MajorityElectionId)
            .IsRequired();

        builder
            .HasMany(p => p.SecondaryMajorityElections)
            .WithOne(sme => sme.PrimaryMajorityElection)
            .HasForeignKey(po => po.PrimaryMajorityElectionId)
            .IsRequired();

        builder
            .HasMany(p => p.SecondaryMajorityElectionsOnSeparateBallots)
            .WithOne(sme => sme.PrimaryMajorityElection)
            .HasForeignKey(po => po.PrimaryMajorityElectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<MajorityElectionTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(b => new { b.MajorityElectionId, b.Language })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<MajorityElectionCandidate> builder)
    {
        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.MajorityElectionCandidate!)
            .HasForeignKey(x => x.MajorityElectionCandidateId);

        builder
            .Property(d => d.DateOfBirth)
            .HasDateType()
            .HasUtcConversion();

        builder
            .HasMany(x => x.CandidateReferencesOfSecondaryElectionsOnSeparateBallot)
            .WithOne(x => x.CandidateReference)
            .HasForeignKey(x => x.CandidateReferenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<MajorityElectionCandidateTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(b => new { b.MajorityElectionCandidateId, b.Language })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<MajorityElectionUnion> builder)
    {
        builder
            .HasOne(mu => mu.Contest)
            .WithMany(c => c.MajorityElectionUnions)
            .HasForeignKey(pu => pu.ContestId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<MajorityElectionUnionEntry> builder)
    {
        builder
            .HasOne(me => me.MajorityElectionUnion)
            .WithMany(mu => mu.MajorityElectionUnionEntries)
            .HasForeignKey(me => me.MajorityElectionUnionId)
            .IsRequired();

        builder
            .HasOne(me => me.MajorityElection)
            .WithMany(mu => mu.MajorityElectionUnionEntries)
            .HasForeignKey(me => me.MajorityElectionId)
            .IsRequired();

        builder
            .HasIndex(me => new { me.MajorityElectionId, me.MajorityElectionUnionId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<SecondaryMajorityElectionCandidate> builder)
    {
        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.SecondaryMajorityElectionCandidate!)
            .HasForeignKey(x => x.SecondaryMajorityElectionCandidateId);

        builder
            .HasOne(c => c.SecondaryMajorityElection)
            .WithMany(c => c.Candidates)
            .HasForeignKey(c => c.SecondaryMajorityElectionId)
            .IsRequired();

        builder
            .HasOne(c => c.CandidateReference)
            .WithMany(c => c!.CandidateReferences)
            .HasForeignKey(c => c.CandidateReferenceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(d => d.DateOfBirth)
            .HasDateType()
            .HasUtcConversion();
    }

    public void Configure(EntityTypeBuilder<SecondaryMajorityElectionCandidateTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(b => new { b.SecondaryMajorityElectionCandidateId, b.Language })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<SecondaryMajorityElection> builder)
    {
        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.SecondaryMajorityElection!)
            .HasForeignKey(x => x.SecondaryMajorityElectionId);

        builder.Ignore(sme => sme.DomainOfInfluenceId);
        builder.Ignore(sme => sme.DomainOfInfluence);
        builder.Ignore(sme => sme.ContestId);
        builder.Ignore(sme => sme.Contest);
    }

    public void Configure(EntityTypeBuilder<SecondaryMajorityElectionTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(b => new { b.SecondaryMajorityElectionId, b.Language })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<MajorityElectionBallotGroup> builder)
    {
        builder
            .HasOne(b => b.MajorityElection)
            .WithMany(m => m.BallotGroups)
            .HasForeignKey(b => b.MajorityElectionId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<MajorityElectionBallotGroupEntry> builder)
    {
        builder
            .HasOne(b => b.BallotGroup)
            .WithMany(m => m.Entries)
            .HasForeignKey(b => b.BallotGroupId)
            .IsRequired();

        builder
            .HasOne(b => b.PrimaryMajorityElection)
            .WithMany(m => m!.BallotGroupEntries)
            .HasForeignKey(b => b.PrimaryMajorityElectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(b => b.SecondaryMajorityElection)
            .WithMany(m => m!.BallotGroupEntries)
            .HasForeignKey(b => b.SecondaryMajorityElectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<MajorityElectionBallotGroupEntryCandidate> builder)
    {
        builder
            .HasOne(b => b.PrimaryElectionCandidate)
            .WithMany(m => m!.BallotGroupEntries)
            .HasForeignKey(b => b.PrimaryElectionCandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(b => b.SecondaryElectionCandidate)
            .WithMany(m => m!.BallotGroupEntries)
            .HasForeignKey(b => b.SecondaryElectionCandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(b => b.BallotGroupEntry)
            .WithMany(m => m.Candidates)
            .HasForeignKey(b => b.BallotGroupEntryId)
            .IsRequired();
    }
}
