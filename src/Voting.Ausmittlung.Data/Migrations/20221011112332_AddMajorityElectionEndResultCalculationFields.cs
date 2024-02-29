// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddMajorityElectionEndResultCalculationFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "Calculation_AbsoluteMajorityThreshold",
            table: "MajorityElectionEndResults",
            type: "numeric",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Calculation_DecisiveVoteCount",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Calculation_AbsoluteMajorityThreshold",
            table: "MajorityElectionEndResults");

        migrationBuilder.DropColumn(
            name: "Calculation_DecisiveVoteCount",
            table: "MajorityElectionEndResults");
    }
}
