// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class VoteResultModelBuilder :
    IEntityTypeConfiguration<VoteResult>,
    IEntityTypeConfiguration<BallotResult>,
    IEntityTypeConfiguration<BallotQuestionResult>,
    IEntityTypeConfiguration<TieBreakQuestionResult>,
    IEntityTypeConfiguration<VoteResultBallot>,
    IEntityTypeConfiguration<VoteResultBundle>,
    IEntityTypeConfiguration<VoteResultBallotQuestionAnswer>,
    IEntityTypeConfiguration<VoteResultBallotTieBreakQuestionAnswer>
{
    public void Configure(EntityTypeBuilder<BallotResult> builder)
    {
        builder
            .HasIndex(br => new { br.BallotId, br.VoteResultId })
            .IsUnique();

        builder
            .HasOne(br => br.Ballot)
            .WithMany(b => b.Results)
            .HasForeignKey(br => br.BallotId)
            .IsRequired();

        builder
            .HasOne(br => br.VoteResult)
            .WithMany(b => b.Results)
            .HasForeignKey(br => br.VoteResultId)
            .IsRequired();

        builder
            .OwnsOne(x => x.CountOfVoters);
        builder
            .Navigation(x => x.CountOfVoters).IsRequired();
    }

    public void Configure(EntityTypeBuilder<VoteResult> builder)
    {
        builder
            .HasIndex(vo => new { vo.VoteId, vo.CountingCircleId })
            .IsUnique();

        builder
            .HasOne(c => c.CountingCircle)
            .WithMany(cc => cc.VoteResults)
            .HasForeignKey(c => c.CountingCircleId)
            .IsRequired();

        builder
            .HasOne(c => c.Vote)
            .WithMany(v => v.Results)
            .HasForeignKey(c => c.VoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(x => x.SubmissionDoneTimestamp)
            .HasUtcConversion();

        builder.OwnsOne(x => x.EntryParams);
    }

    public void Configure(EntityTypeBuilder<BallotQuestionResult> builder)
    {
        builder
            .HasOne(bqr => bqr.Question)
            .WithMany(q => q.Results)
            .HasForeignKey(bqr => bqr.QuestionId)
            .IsRequired();

        builder
            .HasOne(bqr => bqr.BallotResult)
            .WithMany(q => q.QuestionResults)
            .HasForeignKey(bqr => bqr.BallotResultId)
            .IsRequired();

        builder.OwnsOne(x => x.ConventionalSubTotal);
        builder.Navigation(x => x.ConventionalSubTotal).IsRequired();

        builder.OwnsOne(x => x.EVotingSubTotal);
        builder.Navigation(x => x.EVotingSubTotal).IsRequired();
    }

    public void Configure(EntityTypeBuilder<TieBreakQuestionResult> builder)
    {
        builder
            .HasOne(bqr => bqr.Question)
            .WithMany(q => q.Results)
            .HasForeignKey(bqr => bqr.QuestionId)
            .IsRequired();

        builder
            .HasOne(bqr => bqr.BallotResult)
            .WithMany(q => q.TieBreakQuestionResults)
            .HasForeignKey(bqr => bqr.BallotResultId)
            .IsRequired();

        builder.OwnsOne(x => x.ConventionalSubTotal);
        builder.Navigation(x => x.ConventionalSubTotal).IsRequired();

        builder.OwnsOne(x => x.EVotingSubTotal);
        builder.Navigation(x => x.EVotingSubTotal).IsRequired();
    }

    public void Configure(EntityTypeBuilder<VoteResultBallot> builder)
    {
        builder
            .HasOne(x => x.Bundle)
            .WithMany(x => x.Ballots)
            .HasForeignKey(x => x.BundleId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<VoteResultBundle> builder)
    {
        builder
            .HasOne(x => x.BallotResult)
            .WithMany(x => x.Bundles)
            .HasForeignKey(x => x.BallotResultId)
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

    public void Configure(EntityTypeBuilder<VoteResultBallotQuestionAnswer> builder)
    {
        builder
            .HasOne(x => x.Ballot)
            .WithMany(x => x.QuestionAnswers)
            .HasForeignKey(x => x.BallotId)
            .IsRequired();

        builder
            .HasOne(x => x.Question)
            .WithMany(x => x.BallotAnswers)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(x => new { x.BallotId, x.QuestionId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<VoteResultBallotTieBreakQuestionAnswer> builder)
    {
        builder
            .HasOne(x => x.Ballot)
            .WithMany(x => x.TieBreakQuestionAnswers)
            .HasForeignKey(x => x.BallotId)
            .IsRequired();

        builder
            .HasOne(x => x.Question)
            .WithMany(x => x.BallotAnswers)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(x => new { x.BallotId, x.QuestionId })
            .IsUnique();
    }
}
