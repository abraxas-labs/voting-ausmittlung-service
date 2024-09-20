// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class SimplePoliticalBusinessModelBuilder :
    IEntityTypeConfiguration<SimplePoliticalBusiness>,
    IEntityTypeConfiguration<SimplePoliticalBusinessTranslation>,
    IEntityTypeConfiguration<SimpleCountingCircleResult>
{
    public void Configure(EntityTypeBuilder<SimplePoliticalBusiness> builder)
    {
        builder
            .HasMany(x => x.Translations)
            .WithOne(x => x.SimplePoliticalBusiness!)
            .HasForeignKey(x => x.SimplePoliticalBusinessId);

        builder
            .HasOne(v => v.DomainOfInfluence)
            .WithMany(di => di.SimplePoliticalBusinesses)
            .HasForeignKey(v => v.DomainOfInfluenceId)
            .IsRequired();

        builder
            .HasOne(v => v.Contest)
            .WithMany(c => c.SimplePoliticalBusinesses)
            .HasForeignKey(v => v.ContestId)
            .IsRequired();

        builder
            .HasMany(x => x.ResultExportConfigurations)
            .WithOne(x => x.PoliticalBusiness!)
            .HasForeignKey(x => x.PoliticalBusinessId)
            .IsRequired();

        builder
            .Ignore(x => x.SwissAbroadVotingRight);
    }

    public void Configure(EntityTypeBuilder<SimplePoliticalBusinessTranslation> builder)
    {
        builder.HasLanguageQueryFilter();

        builder
            .HasIndex(b => new { b.SimplePoliticalBusinessId, b.Language })
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<SimpleCountingCircleResult> builder)
    {
        builder
            .HasOne(x => x.PoliticalBusiness!)
            .WithMany(x => x.SimpleResults!)
            .HasForeignKey(x => x.PoliticalBusinessId)
            .IsRequired();

        builder
            .HasOne(x => x.CountingCircle!)
            .WithMany(x => x.SimpleResults!)
            .HasForeignKey(x => x.CountingCircleId)
            .IsRequired();

        builder
            .Property(x => x.SubmissionDoneTimestamp)
            .HasUtcConversion();

        builder
            .Property(x => x.AuditedTentativelyTimestamp)
            .HasUtcConversion();

        builder
            .Property(x => x.PlausibilisedTimestamp)
            .HasUtcConversion();

        builder.OwnsOne(x => x.CountOfVoters);
        builder.Navigation(x => x.CountOfVoters).IsRequired();
    }
}
