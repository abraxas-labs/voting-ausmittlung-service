// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class MajorityElectionResultModelBuilder :
    IEntityTypeConfiguration<MajorityElectionResult>,
    IEntityTypeConfiguration<SecondaryMajorityElectionResult>,
    IEntityTypeConfiguration<MajorityElectionCandidateResult>,
    IEntityTypeConfiguration<MajorityElectionBallotGroupResult>,
    IEntityTypeConfiguration<SecondaryMajorityElectionResultBallotCandidate>,
    IEntityTypeConfiguration<SecondaryMajorityElectionResultBallot>,
    IEntityTypeConfiguration<MajorityElectionResultBallotCandidate>,
    IEntityTypeConfiguration<MajorityElectionResultBallot>,
    IEntityTypeConfiguration<MajorityElectionResultBundle>,
    IEntityTypeConfiguration<SecondaryMajorityElectionCandidateResult>,
    IEntityTypeConfiguration<MajorityElectionWriteInBallot>,
    IEntityTypeConfiguration<MajorityElectionWriteInBallotPosition>,
    IEntityTypeConfiguration<SecondaryMajorityElectionWriteInBallot>,
    IEntityTypeConfiguration<SecondaryMajorityElectionWriteInBallotPosition>
{
    public void Configure(EntityTypeBuilder<MajorityElectionResult> builder)
    {
        builder
            .Property(x => x.SubmissionDoneTimestamp)
            .HasUtcConversion();

        builder
            .HasIndex(vo => new { vo.MajorityElectionId, vo.CountingCircleId })
            .IsUnique();

        builder
            .HasOne(c => c.CountingCircle)
            .WithMany(cc => cc.MajorityElectionResults)
            .HasForeignKey(c => c.CountingCircleId)
            .IsRequired();

        builder
            .HasOne(c => c.MajorityElection)
            .WithMany(v => v.Results)
            .HasForeignKey(c => c.MajorityElectionId)
            .IsRequired();

        builder.OwnsOne(x => x.EntryParams);

        builder.OwnsOne(x => x.CountOfVoters);
        builder.Navigation(x => x.CountOfVoters).IsRequired();

        builder.OwnsOne(x => x.ConventionalSubTotal);
        builder.Navigation(x => x.ConventionalSubTotal).IsRequired();

        builder.OwnsOne(x => x.EVotingSubTotal);
        builder.Navigation(x => x.EVotingSubTotal).IsRequired();
    }

    public void Configure(EntityTypeBuilder<SecondaryMajorityElectionResult> builder)
    {
        builder
            .HasOne(c => c.PrimaryResult)
            .WithMany(v => v.SecondaryMajorityElectionResults)
            .HasForeignKey(c => c.PrimaryResultId)
            .IsRequired();

        builder
            .HasOne(c => c.SecondaryMajorityElection)
            .WithMany(v => v.Results)
            .HasForeignKey(c => c.SecondaryMajorityElectionId)
            .IsRequired();

        builder.OwnsOne(x => x.ConventionalSubTotal);
        builder.Navigation(x => x.ConventionalSubTotal).IsRequired();

        builder.OwnsOne(x => x.EVotingSubTotal);
        builder.Navigation(x => x.EVotingSubTotal).IsRequired();
    }

    public void Configure(EntityTypeBuilder<MajorityElectionCandidateResult> builder)
    {
        builder
            .HasOne(x => x.ElectionResult)
            .WithMany(x => x.CandidateResults)
            .HasForeignKey(x => x.ElectionResultId)
            .IsRequired();

        builder
            .HasOne(x => x.Candidate)
            .WithMany(x => x.CandidateResults)
            .HasForeignKey(x => x.CandidateId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.ElectionResultId, x.CandidateId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<MajorityElectionBallotGroupResult> builder)
    {
        builder
            .HasOne(gr => gr.BallotGroup)
            .WithMany(bg => bg.BallotGroupResults)
            .HasForeignKey(gr => gr.BallotGroupId)
            .IsRequired();

        builder
            .HasOne(gr => gr.ElectionResult)
            .WithMany(bg => bg.BallotGroupResults)
            .HasForeignKey(gr => gr.ElectionResultId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.BallotGroupId, x.ElectionResultId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<SecondaryMajorityElectionResultBallotCandidate> builder)
    {
        builder
            .HasOne(x => x.Candidate)
            .WithMany(x => x.BallotCandidatures)
            .HasForeignKey(x => x.CandidateId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.BallotId, x.CandidateId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<SecondaryMajorityElectionResultBallot> builder)
    {
        builder
            .HasOne(x => x.PrimaryBallot)
            .WithMany(x => x.SecondaryMajorityElectionBallots)
            .HasForeignKey(x => x.PrimaryBallotId)
            .IsRequired();

        builder
            .HasOne(x => x.SecondaryMajorityElectionResult)
            .WithMany(x => x.ResultBallots)
            .HasForeignKey(x => x.SecondaryMajorityElectionResultId)
            .IsRequired();

        builder
            .HasIndex(x => new { BallotId = x.PrimaryBallotId, x.SecondaryMajorityElectionResultId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<MajorityElectionResultBallotCandidate> builder)
    {
        builder
            .HasOne(x => x.Ballot)
            .WithMany(x => x.BallotCandidates)
            .HasForeignKey(x => x.BallotId)
            .IsRequired();

        builder
            .HasOne(x => x.Candidate)
            .WithMany(x => x.BallotCandidatures)
            .HasForeignKey(x => x.CandidateId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.BallotId, x.CandidateId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<MajorityElectionResultBallot> builder)
    {
        builder
            .HasOne(x => x.Bundle)
            .WithMany(x => x.Ballots)
            .HasForeignKey(x => x.BundleId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<MajorityElectionResultBundle> builder)
    {
        builder
            .HasOne(x => x.ElectionResult)
            .WithMany(x => x.Bundles)
            .HasForeignKey(x => x.ElectionResultId)
            .IsRequired();

        builder.OwnsOne(
            x => x.CreatedBy,
            u =>
            {
                u.Property(uu => uu.SecureConnectId).IsRequired();
                u.Property(uu => uu.FirstName).IsRequired();
                u.Property(uu => uu.LastName).IsRequired();
            });

        builder.Navigation(x => x.CreatedBy).IsRequired();

        builder.OwnsOne(x => x.ReviewedBy);
    }

    public void Configure(EntityTypeBuilder<SecondaryMajorityElectionCandidateResult> builder)
    {
        builder
            .HasOne(x => x.ElectionResult)
            .WithMany(x => x.CandidateResults)
            .HasForeignKey(x => x.ElectionResultId)
            .IsRequired();

        builder
            .HasOne(x => x.Candidate)
            .WithMany(x => x.CandidateResults)
            .HasForeignKey(x => x.CandidateId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.ElectionResultId, x.CandidateId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<MajorityElectionWriteInBallot> builder)
    {
        builder
            .HasOne(x => x.Result)
            .WithMany(x => x.WriteInBallots)
            .HasForeignKey(x => x.ResultId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<MajorityElectionWriteInBallotPosition> builder)
    {
        builder
            .HasOne(x => x.Ballot)
            .WithMany(x => x.WriteInPositions)
            .HasForeignKey(x => x.BallotId)
            .IsRequired();

        builder
            .HasOne(x => x.WriteInMapping)
            .WithMany(x => x.BallotPositions)
            .HasForeignKey(x => x.WriteInMappingId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<SecondaryMajorityElectionWriteInBallot> builder)
    {
        builder
            .HasOne(x => x.Result)
            .WithMany(x => x.WriteInBallots)
            .HasForeignKey(x => x.ResultId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<SecondaryMajorityElectionWriteInBallotPosition> builder)
    {
        builder
            .HasOne(x => x.Ballot)
            .WithMany(x => x.WriteInPositions)
            .HasForeignKey(x => x.BallotId)
            .IsRequired();

        builder
            .HasOne(x => x.WriteInMapping)
            .WithMany(x => x.BallotPositions)
            .HasForeignKey(x => x.WriteInMappingId)
            .IsRequired();
    }
}
