// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddSecondaryMajorityElectionCalculation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Calculation_AbsoluteMajority",
            table: "SecondaryMajorityElectionEndResults",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "Calculation_AbsoluteMajorityThreshold",
            table: "SecondaryMajorityElectionEndResults",
            type: "numeric",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Calculation_DecisiveVoteCount",
            table: "SecondaryMajorityElectionEndResults",
            type: "integer",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Calculation_AbsoluteMajority",
            table: "SecondaryMajorityElectionEndResults");

        migrationBuilder.DropColumn(
            name: "Calculation_AbsoluteMajorityThreshold",
            table: "SecondaryMajorityElectionEndResults");

        migrationBuilder.DropColumn(
            name: "Calculation_DecisiveVoteCount",
            table: "SecondaryMajorityElectionEndResults");
    }
}
