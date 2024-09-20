// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddPublishState : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "Published",
            table: "VoteResults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "Published",
            table: "SimpleCountingCircleResults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "Published",
            table: "ProportionalElectionResults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "Published",
            table: "MajorityElectionResults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "PublishResultsEnabled",
            table: "ContestCantonDefaults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "PublishResultsEnabled",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Published",
            table: "VoteResults");

        migrationBuilder.DropColumn(
            name: "Published",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "Published",
            table: "ProportionalElectionResults");

        migrationBuilder.DropColumn(
            name: "Published",
            table: "MajorityElectionResults");

        migrationBuilder.DropColumn(
            name: "PublishResultsEnabled",
            table: "ContestCantonDefaults");

        migrationBuilder.DropColumn(
            name: "PublishResultsEnabled",
            table: "CantonSettings");
    }
}
