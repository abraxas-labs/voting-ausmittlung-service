// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class MajorityElectionEndResultModelBuilder :
    IEntityTypeConfiguration<MajorityElectionEndResult>,
    IEntityTypeConfiguration<SecondaryMajorityElectionCandidateEndResult>,
    IEntityTypeConfiguration<SecondaryMajorityElectionEndResult>,
    IEntityTypeConfiguration<MajorityElectionCandidateEndResult>
{
    public void Configure(EntityTypeBuilder<MajorityElectionEndResult> builder)
    {
        builder
            .HasOne(x => x.MajorityElection)
            .WithOne(x => x.EndResult!)
            .HasForeignKey<MajorityElectionEndResult>(x => x.MajorityElectionId)
            .IsRequired();

        builder
            .HasMany(x => x.VotingCards)
            .WithOne()
            .HasForeignKey(x => x.MajorityElectionEndResultId)
            .IsRequired();

        builder
            .HasMany(x => x.CountOfVotersInformationSubTotals)
            .WithOne()
            .HasForeignKey(x => x.MajorityElectionEndResultId)
            .IsRequired();

        builder.OwnsOne(x => x.CountOfVoters);
        builder.Navigation(x => x.CountOfVoters).IsRequired();

        builder.OwnsOne(x => x.Calculation);
        builder.Navigation(x => x.Calculation).IsRequired();

        builder.OwnsOne(x => x.ConventionalSubTotal);
        builder.Navigation(x => x.ConventionalSubTotal).IsRequired();

        builder.OwnsOne(x => x.EVotingSubTotal);
        builder.Navigation(x => x.EVotingSubTotal).IsRequired();
    }

    public void Configure(EntityTypeBuilder<SecondaryMajorityElectionCandidateEndResult> builder)
    {
        builder
            .HasOne(x => x.Candidate)
            .WithOne(x => x.EndResult!)
            .HasForeignKey<SecondaryMajorityElectionCandidateEndResult>(x => x.CandidateId)
            .IsRequired();

        builder
            .HasOne(x => x.SecondaryMajorityElectionEndResult)
            .WithMany(x => x.CandidateEndResults)
            .HasForeignKey(x => x.SecondaryMajorityElectionEndResultId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.CandidateId, x.SecondaryMajorityElectionEndResultId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<SecondaryMajorityElectionEndResult> builder)
    {
        builder
            .HasOne(x => x.SecondaryMajorityElection)
            .WithOne(x => x.EndResult!)
            .HasForeignKey<SecondaryMajorityElectionEndResult>(x => x.SecondaryMajorityElectionId)

            // this is needed due to a limitation in the migration generator, which reuses the same fk names for different fk's
            // if the names need to be truncated
            .HasConstraintName("FK_SecMajElEndResults_SecondaryMajorityElectionId")
            .IsRequired();

        builder
            .HasOne(x => x.PrimaryMajorityElectionEndResult)
            .WithMany(x => x.SecondaryMajorityElectionEndResults)
            .HasForeignKey(x => x.PrimaryMajorityElectionEndResultId)

            // this is needed due to a limitation in the migration generator, which reuses the same fk names for different fk's
            // if the names need to be truncated
            .HasConstraintName("FK_MajorityElections_PrimaryMajorityElectionEndResultId")
            .IsRequired();

        builder
            .HasIndex(x => new { x.SecondaryMajorityElectionId, x.PrimaryMajorityElectionEndResultId })
            .IsUnique();

        builder.OwnsOne(x => x.ConventionalSubTotal);
        builder.Navigation(x => x.ConventionalSubTotal).IsRequired();

        builder.OwnsOne(x => x.EVotingSubTotal);
        builder.Navigation(x => x.EVotingSubTotal).IsRequired();
    }

    public void Configure(EntityTypeBuilder<MajorityElectionCandidateEndResult> builder)
    {
        builder
            .HasOne(x => x.Candidate)
            .WithOne(x => x.EndResult!)
            .HasForeignKey<MajorityElectionCandidateEndResult>(x => x.CandidateId)
            .IsRequired();

        builder
            .HasOne(x => x.MajorityElectionEndResult)
            .WithMany(x => x.CandidateEndResults)
            .HasForeignKey(x => x.MajorityElectionEndResultId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.CandidateId, x.MajorityElectionEndResultId })
            .IsUnique();
    }
}
