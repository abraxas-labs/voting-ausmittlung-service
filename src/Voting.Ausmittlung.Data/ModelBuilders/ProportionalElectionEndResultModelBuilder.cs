// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class ProportionalElectionEndResultModelBuilder :
    IEntityTypeConfiguration<ProportionalElectionEndResult>,
    IEntityTypeConfiguration<ProportionalElectionCandidateEndResult>,
    IEntityTypeConfiguration<ProportionalElectionListEndResult>,
    IEntityTypeConfiguration<ProportionalElectionCandidateVoteSourceEndResult>,
    IEntityTypeConfiguration<HagenbachBischoffGroup>,
    IEntityTypeConfiguration<HagenbachBischoffCalculationRound>,
    IEntityTypeConfiguration<HagenbachBischoffCalculationRoundGroupValues>
{
    public void Configure(EntityTypeBuilder<ProportionalElectionEndResult> builder)
    {
        builder
            .HasOne(x => x.ProportionalElection)
            .WithOne(x => x.EndResult!)
            .HasForeignKey<ProportionalElectionEndResult>(x => x.ProportionalElectionId)
            .IsRequired();

        builder
            .HasMany(x => x.VotingCards)
            .WithOne()
            .HasForeignKey(x => x.ProportionalElectionEndResultId)
            .IsRequired();

        builder
            .HasMany(x => x.CountOfVotersInformationSubTotals)
            .WithOne()
            .HasForeignKey(x => x.ProportionalElectionEndResultId)
            .IsRequired();

        builder.OwnsOne(x => x.CountOfVoters);
        builder.Navigation(x => x.CountOfVoters).IsRequired();

        builder.OwnsOne(x => x.ConventionalSubTotal);
        builder.Navigation(x => x.ConventionalSubTotal).IsRequired();

        builder.OwnsOne(x => x.EVotingSubTotal);
        builder.Navigation(x => x.EVotingSubTotal).IsRequired();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionCandidateEndResult> builder)
    {
        builder
            .HasOne(x => x.Candidate)
            .WithOne(x => x.EndResult!)
            .HasForeignKey<ProportionalElectionCandidateEndResult>(x => x.CandidateId)

            // this is needed due to a limitation in the migration generator, which reuses the same fk names for different fk's
            // if the names need to be truncated
            .HasConstraintName("FK_ProportionalElectionCandidateEndResults_CandidateId")
            .IsRequired();

        builder
            .HasOne(x => x.ListEndResult)
            .WithMany(x => x.CandidateEndResults)
            .HasForeignKey(x => x.ListEndResultId)

            // this is needed due to a limitation in the migration generator, which reuses the same fk names for different fk's
            // if the names need to be truncated
            .HasConstraintName("FK_ProportionalElectionCandidateEndResults_ListEndResultId")
            .IsRequired();

        builder
            .HasIndex(x => new { x.CandidateId, x.ListEndResultId })
            .IsUnique();

        builder.OwnsOne(x => x.EVotingSubTotal);
        builder.Navigation(x => x.EVotingSubTotal).IsRequired();

        builder.OwnsOne(x => x.ConventionalSubTotal);
        builder.Navigation(x => x.ConventionalSubTotal).IsRequired();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionListEndResult> builder)
    {
        builder
            .HasOne(x => x.List)
            .WithOne(x => x.EndResult!)
            .HasForeignKey<ProportionalElectionListEndResult>(x => x.ListId)
            .IsRequired();

        builder
            .HasOne(x => x.ElectionEndResult)
            .WithMany(x => x.ListEndResults)
            .HasForeignKey(x => x.ElectionEndResultId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.ListId, x.ElectionEndResultId })
            .IsUnique();

        builder.OwnsOne(x => x.ConventionalSubTotal);
        builder.Navigation(x => x.ConventionalSubTotal).IsRequired();

        builder.OwnsOne(x => x.EVotingSubTotal);
        builder.Navigation(x => x.EVotingSubTotal).IsRequired();
    }

    public void Configure(EntityTypeBuilder<ProportionalElectionCandidateVoteSourceEndResult> builder)
    {
        builder
            .HasOne(x => x.List)
            .WithMany(x => x.CandidateEndResultVoteSources)
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

    public void Configure(EntityTypeBuilder<HagenbachBischoffGroup> builder)
    {
        builder
            .Property(x => x.AllListNumbers)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder
            .HasOne(x => x.EndResult)
            .WithOne(x => x!.HagenbachBischoffRootGroup!)
            .HasForeignKey<HagenbachBischoffGroup>(x => x.EndResultId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Parent)
            .WithMany(x => x!.Children)
            .HasForeignKey(x => x.ParentId)

            // this is needed due to a limitation in the migration generator, which reuses the same fk names for different fk's
            // if the names need to be truncated
            .HasConstraintName("FK_PropElectionHBGroup_Parent")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.List!)
            .WithOne(x => x!.HagenbachBischoffGroup!)
            .HasForeignKey<HagenbachBischoffGroup>(x => x.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.ListUnion!)
            .WithOne(x => x.HagenbachBischoffGroup!)
            .HasForeignKey<HagenbachBischoffGroup>(x => x.ListUnionId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<HagenbachBischoffCalculationRound> builder)
    {
        builder
            .HasOne(x => x.Winner)
            .WithMany(x => x.CalculationWinnerRounds)
            .HasForeignKey(x => x.WinnerId)
            .IsRequired();

        builder
            .HasMany(x => x.GroupValues)
            .WithOne(x => x.CalculationRound!)
            .HasForeignKey(x => x.CalculationRoundId)
            .IsRequired();

        builder
            .HasOne(x => x.Group)
            .WithMany(x => x.CalculationRounds!)
            .HasForeignKey(x => x.GroupId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.Index, ListGroupId = x.GroupId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<HagenbachBischoffCalculationRoundGroupValues> builder)
    {
        builder
            .HasIndex(x => new { x.GroupId, x.CalculationRoundId })
            .IsUnique();
    }
}
