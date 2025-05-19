// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddCountryStreetAndHouseNumberToElectionCandidate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Country",
            table: "SecondaryMajorityElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: "CH");

        migrationBuilder.AddColumn<string>(
            name: "HouseNumber",
            table: "SecondaryMajorityElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "Street",
            table: "SecondaryMajorityElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "Country",
            table: "ProportionalElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: "CH");

        migrationBuilder.AddColumn<string>(
            name: "HouseNumber",
            table: "ProportionalElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "Street",
            table: "ProportionalElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "Country",
            table: "MajorityElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: "CH");

        migrationBuilder.AddColumn<string>(
            name: "HouseNumber",
            table: "MajorityElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "Street",
            table: "MajorityElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Country",
            table: "SecondaryMajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "HouseNumber",
            table: "SecondaryMajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "Street",
            table: "SecondaryMajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "Country",
            table: "ProportionalElectionCandidates");

        migrationBuilder.DropColumn(
            name: "HouseNumber",
            table: "ProportionalElectionCandidates");

        migrationBuilder.DropColumn(
            name: "Street",
            table: "ProportionalElectionCandidates");

        migrationBuilder.DropColumn(
            name: "Country",
            table: "MajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "HouseNumber",
            table: "MajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "Street",
            table: "MajorityElectionCandidates");
    }
}
