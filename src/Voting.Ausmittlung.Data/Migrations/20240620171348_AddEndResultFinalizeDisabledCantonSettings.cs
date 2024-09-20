// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddEndResultFinalizeDisabledCantonSettings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "MandateDistributionTriggered",
            table: "ProportionalElectionEndResult",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "EndResultFinalizeDisabled",
            table: "ContestCantonDefaults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "EndResultFinalizeDisabled",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql(@"
                UPDATE ""ProportionalElectionEndResult""
                SET ""MandateDistributionTriggered"" = true
                FROM (
                    SELECT ""Id"", ""MandateAlgorithm""
                    FROM ""ProportionalElections""
                ) as pe
                WHERE  ""TotalCountOfCountingCircles"" = ""CountOfDoneCountingCircles""
                    AND pe.""Id"" = ""ProportionalElectionEndResult"".""ProportionalElectionId""
                    AND (pe.""MandateAlgorithm"" = 1 OR pe.""MandateAlgorithm"" = 6)
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MandateDistributionTriggered",
            table: "ProportionalElectionEndResult");

        migrationBuilder.DropColumn(
            name: "EndResultFinalizeDisabled",
            table: "ContestCantonDefaults");

        migrationBuilder.DropColumn(
            name: "EndResultFinalizeDisabled",
            table: "CantonSettings");
    }
}
