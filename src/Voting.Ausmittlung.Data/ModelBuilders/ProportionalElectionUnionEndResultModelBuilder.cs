// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class ProportionalElectionUnionEndResultModelBuilder : IEntityTypeConfiguration<ProportionalElectionUnionEndResult>
{
    public void Configure(EntityTypeBuilder<ProportionalElectionUnionEndResult> builder)
    {
        builder
            .HasOne(x => x.ProportionalElectionUnion)
            .WithOne(x => x.EndResult!)
            .HasForeignKey<ProportionalElectionUnionEndResult>(x => x.ProportionalElectionUnionId)
            .IsRequired();
    }
}
