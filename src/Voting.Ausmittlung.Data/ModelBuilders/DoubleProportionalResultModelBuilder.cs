// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class DoubleProportionalResultModelBuilder : IEntityTypeConfiguration<DoubleProportionalResult>,
    IEntityTypeConfiguration<DoubleProportionalResultColumn>,
    IEntityTypeConfiguration<DoubleProportionalResultRow>,
    IEntityTypeConfiguration<DoubleProportionalResultCell>
{
    public void Configure(EntityTypeBuilder<DoubleProportionalResult> builder)
    {
        builder
            .HasOne(x => x.ProportionalElectionUnion)
            .WithOne(x => x.DoubleProportionalResult)
            .HasForeignKey<DoubleProportionalResult>(x => x.ProportionalElectionUnionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.ProportionalElection)
            .WithOne(x => x.DoubleProportionalResult)
            .HasForeignKey<DoubleProportionalResult>(x => x.ProportionalElectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<DoubleProportionalResultRow> builder)
    {
        builder
            .HasOne(x => x.ProportionalElection)
            .WithMany(x => x.DoubleProportionalResultRows)
            .HasForeignKey(x => x.ProportionalElectionId)
            .IsRequired();

        builder
            .HasOne(x => x.Result)
            .WithMany(x => x.Rows)
            .HasForeignKey(x => x.ResultId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<DoubleProportionalResultColumn> builder)
    {
        builder
            .HasOne(x => x.UnionList)
            .WithOne(x => x.DoubleProportionalResultColumn)
            .HasForeignKey<DoubleProportionalResultColumn>(x => x.UnionListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.List)
            .WithOne(x => x.DoubleProportionalResultColumn)
            .HasForeignKey<DoubleProportionalResultColumn>(x => x.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Result)
            .WithMany(x => x.Columns)
            .HasForeignKey(x => x.ResultId)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<DoubleProportionalResultCell> builder)
    {
        builder
            .HasOne(x => x.List)
            .WithMany(x => x.DoubleProportionalResultCells)
            .HasForeignKey(x => x.ListId)
            .IsRequired();

        builder
            .HasOne(x => x.Row)
            .WithMany(x => x.Cells)
            .HasForeignKey(x => x.RowId)
            .IsRequired();

        builder
            .HasOne(x => x.Column)
            .WithMany(x => x.Cells)
            .HasForeignKey(x => x.ColumnId)
            .IsRequired();
    }
}
