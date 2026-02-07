// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddAutomaticBallotNumberGeneration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "AutomaticBallotNumberGeneration",
            table: "Votes",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql(
            """
            UPDATE "Votes"
            SET "AutomaticBallotNumberGeneration" = TRUE
            """);

        migrationBuilder.AddColumn<bool>(
            name: "EntryParams_AutomaticBallotNumberGeneration",
            table: "VoteResults",
            type: "boolean",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "VoteResults"
            SET "EntryParams_AutomaticBallotNumberGeneration" = TRUE
            """);

        migrationBuilder.AddColumn<bool>(
            name: "AutomaticBallotNumberGeneration",
            table: "ProportionalElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql(
            """
            UPDATE "ProportionalElections"
            SET "AutomaticBallotNumberGeneration" = TRUE
            """);

        migrationBuilder.AddColumn<bool>(
            name: "EntryParams_AutomaticBallotNumberGeneration",
            table: "ProportionalElectionResults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql(
            """
            UPDATE "ProportionalElectionResults"
            SET "EntryParams_AutomaticBallotNumberGeneration" = TRUE
            """);

        migrationBuilder.AddColumn<bool>(
            name: "AutomaticBallotNumberGeneration",
            table: "MajorityElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql(
            """
            UPDATE "MajorityElections"
            SET "AutomaticBallotNumberGeneration" = TRUE
            """);

        migrationBuilder.AddColumn<bool>(
            name: "EntryParams_AutomaticBallotNumberGeneration",
            table: "MajorityElectionResults",
            type: "boolean",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "MajorityElectionResults"
            SET "EntryParams_AutomaticBallotNumberGeneration" = TRUE
            """);

        migrationBuilder.AddColumn<int>(
            name: "Index",
            table: "VoteResultBallots",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql(
            """
            UPDATE "VoteResultBallots"
            SET "Index" = "Number"
            """);

        migrationBuilder.AddColumn<int>(
            name: "Index",
            table: "ProportionalElectionResultBallots",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql(
            """
            UPDATE "ProportionalElectionResultBallots"
            SET "Index" = "Number"
            """);

        migrationBuilder.AddColumn<int>(
            name: "Index",
            table: "MajorityElectionResultBallots",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql(
            """
            UPDATE "MajorityElectionResultBallots"
            SET "Index" = "Number"
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AutomaticBallotNumberGeneration",
            table: "Votes");

        migrationBuilder.DropColumn(
            name: "EntryParams_AutomaticBallotNumberGeneration",
            table: "VoteResults");

        migrationBuilder.DropColumn(
            name: "AutomaticBallotNumberGeneration",
            table: "ProportionalElections");

        migrationBuilder.DropColumn(
            name: "EntryParams_AutomaticBallotNumberGeneration",
            table: "ProportionalElectionResults");

        migrationBuilder.DropColumn(
            name: "AutomaticBallotNumberGeneration",
            table: "MajorityElections");

        migrationBuilder.DropColumn(
            name: "EntryParams_AutomaticBallotNumberGeneration",
            table: "MajorityElectionResults");

        migrationBuilder.DropColumn(
            name: "Index",
            table: "VoteResultBallots");

        migrationBuilder.DropColumn(
            name: "Index",
            table: "ProportionalElectionResultBallots");

        migrationBuilder.DropColumn(
            name: "Index",
            table: "MajorityElectionResultBallots");
    }
}
