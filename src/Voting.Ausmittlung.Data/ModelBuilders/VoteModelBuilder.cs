// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class VoteModelBuilder :
    IEntityTypeConfiguration<Vote>,
    IEntityTypeConfiguration<VoteTranslation>,
    IEntityTypeConfiguration<Ballot>,
    IEntityTypeConfiguration<BallotQuestion>,
    IEntityTypeConfiguration<BallotTranslation>,
    IEntityTypeConfiguration<BallotQuestionTranslation>,
    IEntityTypeConfiguration<TieBreakQuestion>,
    IEntityTypeConfiguration<TieBreakQuestionTranslation>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.Vote!)
            .HasForeignKey(x => x.VoteId);

        builder
            .HasOne(v => v.DomainOfInfluence)
            .WithMany(di => di.Votes)
            .HasForeignKey(v => v.DomainOfInfluenceId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(v => v.Contest)
            .WithMany(c => c.Votes)
            .HasForeignKey(v => v.ContestId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasMany(v => v.Results)
            .WithOne(vo => vo.Vote)
            .HasForeignKey(vo => vo.VoteId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasMany(v => v.Ballots)
            .WithOne(b => b.Vote)
            .HasForeignKey(b => b.VoteId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<VoteTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(b => new { b.VoteId, b.Language })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<Ballot> builder)
    {
        builder
            .HasMany(b => b.BallotQuestions)
            .WithOne(bq => bq.Ballot)
            .HasForeignKey(bq => bq.BallotId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasMany(b => b.TieBreakQuestions)
            .WithOne(bq => bq.Ballot)
            .HasForeignKey(bq => bq.BallotId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.Ballot!)
            .HasForeignKey(x => x.BallotId);

        builder
            .HasIndex(b => new { b.VoteId, b.Position })
            .IsUnique();

        builder
            .Property(x => x.HasTieBreakQuestions)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<BallotQuestion> builder)
    {
        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.BallotQuestion!)
            .HasForeignKey(x => x.BallotQuestionId);

        builder
            .HasIndex(bq => new { bq.Number, bq.BallotId })
            .IsUnique();

        builder
            .Property(x => x.Number)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<BallotTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(b => new { b.BallotId, b.Language })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<BallotQuestionTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(b => new { b.BallotQuestionId, b.Language })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<TieBreakQuestion> builder)
    {
        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.TieBreakQuestion!)
            .HasForeignKey(x => x.TieBreakQuestionId);

        builder
            .HasIndex(tbq => new
            {
                tbq.BallotId,
                QuestionNumber1 = tbq.Question1Number,
                QuestionNumber2 = tbq.Question2Number,
            })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<TieBreakQuestionTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(b => new { b.TieBreakQuestionId, b.Language })
            .IsUnique();
    }
}
