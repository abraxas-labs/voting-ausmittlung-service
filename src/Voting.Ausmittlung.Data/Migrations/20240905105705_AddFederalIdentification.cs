// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddFederalIdentification : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "FederalIdentification",
            table: "ProportionalElections",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "FederalIdentification",
            table: "MajorityElections",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "FederalIdentification",
            table: "Ballots",
            type: "integer",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "FederalIdentification",
            table: "ProportionalElections");

        migrationBuilder.DropColumn(
            name: "FederalIdentification",
            table: "MajorityElections");

        migrationBuilder.DropColumn(
            name: "FederalIdentification",
            table: "Ballots");
    }
}
