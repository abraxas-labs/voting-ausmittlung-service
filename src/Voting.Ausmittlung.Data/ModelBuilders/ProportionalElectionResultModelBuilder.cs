// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class ProportionalElectionResultModelBuilder :
    IEntityTypeConfiguration<ProportionalElectionResult>,
    IEntityTypeConfiguration<ProportionalElectionUnmodifiedListResult>,
    IEntityTypeConfiguration<ProportionalElectionListResult>,
    IEntityTypeConfiguration<ProportionalElectionCandidateResult>,
    IEntityTypeConfiguration<ProportionalElectionResultBundle>,
    IEntityTypeConfiguration<ProportionalElectionResultBallot>,
    IEntityTypeConfiguration<ProportionalElectionResultBallotCandidate>,
    IEntityTypeConfiguration<ProportionalElectionCandidateVoteSourceResult>
{
    public void Configure(EntityTypeBuilder<ProportionalElectionResult> builder)
    {
        builder
            .HasIndex(vo => new { vo.ProportionalElectionId, vo.CountingCircleId })
            .IsUnique();

        builder
            .HasOne(c => c.CountingCircle)
            .WithMany(cc => cc.ProportionalElectionResults)
            .HasForeignKey(c => c.CountingCircleId)
            .IsRequired();

        builder
            .HasOne(c => c.ProportionalElection)
            .WithMany(v => v.Results)
            .HasForeignKey(c => c.ProportionalElectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(x => x.SubmissionDoneTimestamp)
            .HasUtcConversion();

        builder
            .Property(x => x.AuditedTentativelyTimestamp)
            .HasUtcConversion();

        builder
            .Property(x => x.PlausibilisedTimestamp)
            .HasUtcConversion();

        builder.OwnsOne(x => x.EntryParams);
        builder.Navigation(x => x.EntryParams).IsRequired();

        builder.OwnsOne(x => x.CountOfVoters);
        builder.Navigation(x => x.CountOfVoters).IsRequired();

        builder.OwnsOne(x => x.EVotingSubTotal);
        builder.Navigation(x => x.EVotingSubTotal).IsRequired();

        builder.OwnsOne(x => x.ConventionalSubTotal);
        builder.Navigation(x => x.ConventionalSubTotal).IsRequired();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionUnmodifiedListResult> builder)
    {
        builder
            .HasOne(x => x.List)
            .WithMany(x => x.UnmodifiedListResults)
            .HasForeignKey(x => x.ListId)
            .IsRequired();

        builder
            .HasOne(x => x.Result)
            .WithMany(x => x.UnmodifiedListResults)
            .HasForeignKey(x => x.ResultId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.ResultId, x.ListId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionListResult> builder)
    {
        builder
            .HasOne(x => x.List)
            .WithMany(x => x.Results)
            .HasForeignKey(x => x.ListId)
            .IsRequired();

        builder
            .HasOne(x => x.Result)
            .WithMany(x => x.ListResults)
            .HasForeignKey(x => x.ResultId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.ResultId, x.ListId })
            .IsUnique();

        builder.OwnsOne(x => x.ConventionalSubTotal);
        builder.Navigation(x => x.ConventionalSubTotal).IsRequired();

        builder.OwnsOne(x => x.EVotingSubTotal);
        builder.Navigation(x => x.EVotingSubTotal).IsRequired();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionCandidateResult> builder)
    {
        builder
            .HasOne(x => x.Candidate)
            .WithMany(x => x.Results)
            .HasForeignKey(x => x.CandidateId)

            // this is needed due to a limitation in the migration generator, which reuses the same fk names for different fk's
            // if the names need to be truncated
            .HasConstraintName("FK_ProportionalElectionCandidateResults_CandidateId")
            .IsRequired();

        builder
            .HasOne(x => x.ListResult)
            .WithMany(x => x.CandidateResults)
            .HasForeignKey(x => x.ListResultId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.CandidateId, x.ListResultId })
            .IsUnique();

        builder.OwnsOne(x => x.ConventionalSubTotal);
        builder.Navigation(x => x.ConventionalSubTotal).IsRequired();

        builder.OwnsOne(x => x.EVotingSubTotal);
        builder.Navigation(x => x.EVotingSubTotal).IsRequired();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionResultBundle> builder)
    {
        builder
            .HasOne(x => x.List)
            .WithMany(x => x!.Bundles)
            .HasForeignKey(x => x.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.ElectionResult)
            .WithMany(x => x.Bundles)
            .HasForeignKey(x => x.ElectionResultId)
            .IsRequired();

        builder
            .OwnsOne(
                x => x.CreatedBy,
                u =>
                {
                    u.Property(uu => uu.SecureConnectId).IsRequired();
                    u.Property(uu => uu.FirstName).IsRequired();
                    u.Property(uu => uu.LastName).IsRequired();
                });
        builder
            .Navigation(x => x.CreatedBy).IsRequired();

        builder
            .OwnsOne(x => x.ReviewedBy);
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionResultBallot> builder)
    {
        builder
            .HasOne(x => x.Bundle)
            .WithMany(x => x.Ballots)
            .HasForeignKey(x => x.BundleId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionResultBallotCandidate> builder)
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
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionCandidateVoteSourceResult> builder)
    {
        builder
            .HasOne(x => x.List)
            .WithMany(x => x!.CandidateResultVoteSources)
            .HasForeignKey(x => x.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.CandidateResult)
            .WithMany(x => x.VoteSources)
            .HasForeignKey(x => x.CandidateResultId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.CandidateResultId, x.ListId })
            .IsUnique();
    }
}
