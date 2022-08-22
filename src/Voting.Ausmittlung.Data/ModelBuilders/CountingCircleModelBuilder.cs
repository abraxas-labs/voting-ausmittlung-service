// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class CountingCircleModelBuilder : IEntityTypeConfiguration<CountingCircle>
{
    public void Configure(EntityTypeBuilder<CountingCircle> builder)
    {
        builder
            .HasOne(cc => cc.ResponsibleAuthority!)
            .WithOne(a => a.CountingCircle!)
            .HasForeignKey<Authority>(e => e.CountingCircleId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(cc => cc.ContactPersonDuringEvent!)
            .WithOne(cp => cp.CountingCircleDuringEvent!)
            .HasForeignKey<CountingCircleContactPerson>(e => e.CountingCircleDuringEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(cc => cc.ContactPersonAfterEvent!)
            .WithOne(cp => cp!.CountingCircleAfterEvent!)
            .HasForeignKey<CountingCircleContactPerson>(e => e.CountingCircleAfterEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(di => di.SnapshotContest!)
            .WithMany()
            .HasForeignKey(di => di.SnapshotContestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Ignore(di => di.IsSnapshot);
    }
}
