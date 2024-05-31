// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddAdditionalDoubleProportionalProps : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SubApportionmentNumberOfMandates",
            table: "DoubleProportionalResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "SuperApportionmentNumberOfMandates",
            table: "DoubleProportionalResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<decimal>(
            name: "VoterNumber",
            table: "DoubleProportionalResultRows",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SubApportionmentNumberOfMandates",
            table: "DoubleProportionalResults");

        migrationBuilder.DropColumn(
            name: "SuperApportionmentNumberOfMandates",
            table: "DoubleProportionalResults");

        migrationBuilder.DropColumn(
            name: "VoterNumber",
            table: "DoubleProportionalResultRows");
    }
}
