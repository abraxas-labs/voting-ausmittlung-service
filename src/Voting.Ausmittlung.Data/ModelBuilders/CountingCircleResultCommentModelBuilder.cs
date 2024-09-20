// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class CountingCircleResultCommentModelBuilder : IEntityTypeConfiguration<CountingCircleResultComment>
{
    public void Configure(EntityTypeBuilder<CountingCircleResultComment> builder)
    {
        builder
            .HasOne(x => x.Result)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.ResultId)
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

        builder.Navigation(x => x.CreatedBy).IsRequired();

        builder
            .Property(x => x.CreatedAt)
            .HasUtcConversion();
    }
}
