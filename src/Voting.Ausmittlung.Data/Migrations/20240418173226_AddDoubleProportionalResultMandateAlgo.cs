// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddDoubleProportionalResultMandateAlgo : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "AnyQuorumReached",
            table: "DoubleProportionalResultColumns",
            newName: "AnyRequiredQuorumReached");

        migrationBuilder.AddColumn<int>(
            name: "MandateAlgorithm",
            table: "DoubleProportionalResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MandateAlgorithm",
            table: "DoubleProportionalResults");

        migrationBuilder.RenameColumn(
            name: "AnyRequiredQuorumReached",
            table: "DoubleProportionalResultColumns",
            newName: "AnyQuorumReached");
    }
}
