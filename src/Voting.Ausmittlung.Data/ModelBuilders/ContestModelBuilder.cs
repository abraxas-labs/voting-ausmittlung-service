// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class ContestModelBuilder :
    IEntityTypeConfiguration<Contest>,
    IEntityTypeConfiguration<ContestTranslation>,
    IEntityTypeConfiguration<ContestCountingCircleDetails>,
    IEntityTypeConfiguration<ContestCountingCircleElectorate>,
    IEntityTypeConfiguration<CountOfVotersInformationSubTotal>,
    IEntityTypeConfiguration<VotingCardResultDetail>,
    IEntityTypeConfiguration<ContestDetails>,
    IEntityTypeConfiguration<ContestCountOfVotersInformationSubTotal>,
    IEntityTypeConfiguration<ContestVotingCardResultDetail>,
    IEntityTypeConfiguration<ContestDomainOfInfluenceDetails>,
    IEntityTypeConfiguration<DomainOfInfluenceCountOfVotersInformationSubTotal>,
    IEntityTypeConfiguration<DomainOfInfluenceVotingCardResultDetail>,
    IEntityTypeConfiguration<ContestCantonDefaults>
{
    public void Configure(EntityTypeBuilder<Contest> builder)
    {
        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.Contest!)
            .HasForeignKey(x => x.ContestId);

        builder
            .Property(d => d.Date)
            .HasDateType()
            .HasUtcConversion();

        builder
            .Property(d => d.EndOfTestingPhase)
            .HasUtcConversion();

        builder
            .Property(d => d.EVotingFrom)
            .HasUtcConversion();

        builder
            .Property(d => d.EVotingTo)
            .HasUtcConversion();

        builder
            .HasOne(c => c.DomainOfInfluence)
            .WithMany(doi => doi.Contests)
            .HasForeignKey(c => c.DomainOfInfluenceId)
            .IsRequired();

        builder.HasIndex(x => x.State);

        builder
            .HasOne(c => c.PreviousContest)
            .WithMany(c => c!.PreviousContestOwners)
            .HasForeignKey(c => c.PreviousContestId);

        builder
            .HasOne(c => c.CantonDefaults)
            .WithOne(c => c.Contest)
            .HasForeignKey<ContestCantonDefaults>(c => c.ContestId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<ContestTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(b => new { b.ContestId, b.Language })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ContestCountingCircleDetails> builder)
    {
        builder
            .HasOne(x => x.Contest)
            .WithMany(x => x.CountingCircleDetails)
            .HasForeignKey(x => x.ContestId)
            .IsRequired();

        builder
            .HasOne(x => x.CountingCircle)
            .WithMany(x => x.ContestDetails)
            .HasForeignKey(x => x.CountingCircleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(x => new { x.ContestId, x.CountingCircleId })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<CountOfVotersInformationSubTotal> builder)
    {
        builder
            .HasOne(x => x.ContestCountingCircleDetails)
            .WithMany(x => x.CountOfVotersInformationSubTotals)
            .HasForeignKey(x => x.ContestCountingCircleDetailsId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(x => new { x.ContestCountingCircleDetailsId, x.Sex, x.VoterType })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<VotingCardResultDetail> builder)
    {
        builder
            .HasOne(x => x.ContestCountingCircleDetails)
            .WithMany(x => x.VotingCards)
            .HasForeignKey(x => x.ContestCountingCircleDetailsId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.ContestCountingCircleDetailsId, x.Channel, x.Valid, x.DomainOfInfluenceType })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ContestDetails> builder)
    {
        builder
            .HasOne(x => x.Contest)
            .WithOne(x => x.Details!)
            .HasForeignKey<ContestDetails>(x => x.ContestId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<ContestCountOfVotersInformationSubTotal> builder)
    {
        builder
            .HasOne(x => x.ContestDetails)
            .WithMany(x => x.CountOfVotersInformationSubTotals)
            .HasForeignKey(x => x.ContestDetailsId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.ContestDetailsId, x.Sex, x.VoterType })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ContestVotingCardResultDetail> builder)
    {
        builder
            .HasIndex(x => new { x.ContestDetailsId, x.Channel, x.Valid, x.DomainOfInfluenceType })
            .IsUnique();

        builder
            .HasOne(x => x.ContestDetails)
            .WithMany(x => x.VotingCards)
            .HasForeignKey(x => x.ContestDetailsId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<ContestDomainOfInfluenceDetails> builder)
    {
        builder
            .HasOne(x => x.DomainOfInfluence)
            .WithOne(x => x.Details)
            .HasForeignKey<ContestDomainOfInfluenceDetails>(x => x.DomainOfInfluenceId)
            .IsRequired();

        builder
            .HasOne(x => x.Contest)
            .WithMany(x => x.DomainOfInfluenceDetails)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<DomainOfInfluenceCountOfVotersInformationSubTotal> builder)
    {
        builder
            .HasOne(x => x.ContestDomainOfInfluenceDetails)
            .WithMany(x => x.CountOfVotersInformationSubTotals)
            .HasForeignKey(x => x.ContestDomainOfInfluenceDetailsId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.ContestDomainOfInfluenceDetailsId, x.Sex, x.VoterType })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<DomainOfInfluenceVotingCardResultDetail> builder)
    {
        builder
            .HasOne(x => x.ContestDomainOfInfluenceDetails)
            .WithMany(x => x.VotingCards)
            .HasForeignKey(x => x.ContestDomainOfInfluenceDetailsId)
            .IsRequired();

        builder
            .HasIndex(x => new { x.ContestDomainOfInfluenceDetailsId, x.Channel, x.Valid, x.DomainOfInfluenceType })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<ContestCountingCircleElectorate> builder)
    {
        builder
            .HasOne(x => x.Contest)
            .WithMany()
            .HasForeignKey(x => x.ContestId)
            .IsRequired();

        builder
            .HasOne(x => x.CountingCircle)
            .WithMany(x => x.ContestElectorates)
            .HasForeignKey(x => x.CountingCircleId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<ContestCantonDefaults> builder)
    {
        builder.OwnsMany(x => x.CountingCircleResultStateDescriptions);
        builder.OwnsMany(x => x.EnabledVotingCardChannels);
    }
}
