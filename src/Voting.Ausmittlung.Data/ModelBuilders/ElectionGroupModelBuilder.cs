// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class ElectionGroupModelBuilder : IEntityTypeConfiguration<ElectionGroup>
{
    public void Configure(EntityTypeBuilder<ElectionGroup> builder)
    {
        builder
            .HasOne(eg => eg.PrimaryMajorityElection)
            .WithOne(m => m.ElectionGroup!)
            .HasForeignKey<ElectionGroup>(eg => eg.PrimaryMajorityElectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(eg => eg.SecondaryMajorityElections)
            .WithOne(sme => sme.ElectionGroup)
            .HasForeignKey(sme => sme.ElectionGroupId)
            .IsRequired();
    }
}
