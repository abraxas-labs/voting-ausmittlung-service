// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddReviewProcedure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "EnforceReviewProcedureForCountingCircles",
            table: "Votes",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "ReviewProcedure",
            table: "Votes",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<int>(
            name: "EntryParams_ReviewProcedure",
            table: "VoteResults",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "EnforceReviewProcedureForCountingCircles",
            table: "ProportionalElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "ReviewProcedure",
            table: "ProportionalElections",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<int>(
            name: "EntryParams_ReviewProcedure",
            table: "ProportionalElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<bool>(
            name: "EnforceReviewProcedureForCountingCircles",
            table: "MajorityElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "ReviewProcedure",
            table: "MajorityElections",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<int>(
            name: "EntryParams_ReviewProcedure",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EnforceReviewProcedureForCountingCircles",
            table: "Votes");

        migrationBuilder.DropColumn(
            name: "ReviewProcedure",
            table: "Votes");

        migrationBuilder.DropColumn(
            name: "EntryParams_ReviewProcedure",
            table: "VoteResults");

        migrationBuilder.DropColumn(
            name: "EnforceReviewProcedureForCountingCircles",
            table: "ProportionalElections");

        migrationBuilder.DropColumn(
            name: "ReviewProcedure",
            table: "ProportionalElections");

        migrationBuilder.DropColumn(
            name: "EntryParams_ReviewProcedure",
            table: "ProportionalElectionResults");

        migrationBuilder.DropColumn(
            name: "EnforceReviewProcedureForCountingCircles",
            table: "MajorityElections");

        migrationBuilder.DropColumn(
            name: "ReviewProcedure",
            table: "MajorityElections");

        migrationBuilder.DropColumn(
            name: "EntryParams_ReviewProcedure",
            table: "MajorityElectionResults");
    }
}
