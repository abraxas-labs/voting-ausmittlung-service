// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class RemoveInvalidVotesInternalDescriptionIndividualEmptyBallotsAllowed : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "InternalDescription",
            table: "SimplePoliticalBusinesses");

        migrationBuilder.DropColumn(
            name: "InternalDescription",
            table: "SecondaryMajorityElections");

        migrationBuilder.DropColumn(
            name: "InternalDescription",
            table: "ProportionalElections");

        migrationBuilder.DropColumn(
            name: "IndividualEmptyBallotsAllowed",
            table: "MajorityElections");

        migrationBuilder.DropColumn(
            name: "InternalDescription",
            table: "MajorityElections");

        migrationBuilder.DropColumn(
            name: "InvalidVotes",
            table: "MajorityElections");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "InternalDescription",
            table: "SimplePoliticalBusinesses",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "InternalDescription",
            table: "SecondaryMajorityElections",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "InternalDescription",
            table: "ProportionalElections",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<bool>(
            name: "IndividualEmptyBallotsAllowed",
            table: "MajorityElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "InternalDescription",
            table: "MajorityElections",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<bool>(
            name: "InvalidVotes",
            table: "MajorityElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }
}
