// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class VoteEndResultModelBuilder :
    IEntityTypeConfiguration<VoteEndResult>,
    IEntityTypeConfiguration<BallotEndResult>,
    IEntityTypeConfiguration<BallotQuestionEndResult>,
    IEntityTypeConfiguration<TieBreakQuestionEndResult>
{
    public void Configure(EntityTypeBuilder<VoteEndResult> builder)
    {
        builder
            .HasOne(v => v.Vote)
            .WithOne(v => v.EndResult!)
            .HasForeignKey<VoteEndResult>(vo => vo.VoteId)
            .IsRequired();

        builder
            .HasMany(x => x.VotingCards)
            .WithOne()
            .HasForeignKey(x => x.VoteEndResultId)
            .IsRequired();

        builder
            .HasMany(x => x.CountOfVotersInformationSubTotals)
            .WithOne()
            .HasForeignKey(x => x.VoteEndResultId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<BallotEndResult> builder)
    {
        builder
            .HasIndex(br => new { br.BallotId, br.VoteEndResultId })
            .IsUnique();

        builder
            .HasOne(br => br.Ballot)
            .WithOne(b => b.EndResult!)
            .HasForeignKey<BallotEndResult>(br => br.BallotId)
            .IsRequired();

        builder
            .HasOne(br => br.VoteEndResult)
            .WithMany(b => b.BallotEndResults)
            .HasForeignKey(br => br.VoteEndResultId)
            .IsRequired();

        builder
            .OwnsOne(x => x.CountOfVoters);
        builder
            .Navigation(x => x.CountOfVoters).IsRequired();
    }

    public void Configure(EntityTypeBuilder<BallotQuestionEndResult> builder)
    {
        builder
            .HasOne(bqr => bqr.Question)
            .WithOne(q => q.EndResult!)
            .HasForeignKey<BallotQuestionEndResult>(bqr => bqr.QuestionId)
            .IsRequired();

        builder
            .HasOne(bqr => bqr.BallotEndResult)
            .WithMany(q => q.QuestionEndResults)
            .HasForeignKey(bqr => bqr.BallotEndResultId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.BallotEndResultId, x.QuestionId })
            .IsUnique();

        builder.OwnsOne(x => x.ConventionalSubTotal);
        builder.Navigation(x => x.ConventionalSubTotal).IsRequired();

        builder.OwnsOne(x => x.EVotingSubTotal);
        builder.Navigation(x => x.EVotingSubTotal).IsRequired();
    }

    public void Configure(EntityTypeBuilder<TieBreakQuestionEndResult> builder)
    {
        builder
            .HasOne(bqr => bqr.Question)
            .WithOne(q => q.EndResult!)
            .HasForeignKey<TieBreakQuestionEndResult>(bqr => bqr.QuestionId)
            .IsRequired();

        builder
            .HasOne(bqr => bqr.BallotEndResult)
            .WithMany(q => q.TieBreakQuestionEndResults)
            .HasForeignKey(bqr => bqr.BallotEndResultId)
            .IsRequired();

        builder.OwnsOne(x => x.ConventionalSubTotal);
        builder.Navigation(x => x.ConventionalSubTotal).IsRequired();

        builder.OwnsOne(x => x.EVotingSubTotal);
        builder.Navigation(x => x.EVotingSubTotal).IsRequired();
    }
}
