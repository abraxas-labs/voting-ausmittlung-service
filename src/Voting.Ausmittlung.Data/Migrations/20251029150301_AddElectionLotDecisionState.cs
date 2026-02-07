// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddElectionLotDecisionState : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HasOpenRequiredLotDecisions",
            table: "ProportionalElectionListEndResult");

        migrationBuilder.AddColumn<int>(
            name: "LotDecisionState",
            table: "ProportionalElectionListEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "LotDecisionState",
            table: "ProportionalElectionListEndResult");

        migrationBuilder.AddColumn<bool>(
            name: "HasOpenRequiredLotDecisions",
            table: "ProportionalElectionListEndResult",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }
}
