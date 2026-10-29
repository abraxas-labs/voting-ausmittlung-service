// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class MajorityElectionBundlesWithBallotGroupOption : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "BundlesWithBallotGroupEnabled",
            table: "MajorityElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "EnforceBundlesWithBallotGroupEnabledForCountingCircles",
            table: "MajorityElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "MajorityElectionBundlesWithBallotGroupEnabled",
            table: "ContestCantonDefaults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "MajorityElectionBundlesWithBallotGroupEnabled",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BundlesWithBallotGroupEnabled",
            table: "MajorityElections");

        migrationBuilder.DropColumn(
            name: "EnforceBundlesWithBallotGroupEnabledForCountingCircles",
            table: "MajorityElections");

        migrationBuilder.DropColumn(
            name: "MajorityElectionBundlesWithBallotGroupEnabled",
            table: "ContestCantonDefaults");

        migrationBuilder.DropColumn(
            name: "MajorityElectionBundlesWithBallotGroupEnabled",
            table: "CantonSettings");
    }
}
