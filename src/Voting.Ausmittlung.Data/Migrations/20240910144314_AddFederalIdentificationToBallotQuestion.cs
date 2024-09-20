// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddFederalIdentificationToBallotQuestion : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "FederalIdentification",
            table: "Ballots");

        migrationBuilder.AddColumn<int>(
            name: "FederalIdentification",
            table: "TieBreakQuestions",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "FederalIdentification",
            table: "BallotQuestions",
            type: "integer",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "FederalIdentification",
            table: "TieBreakQuestions");

        migrationBuilder.DropColumn(
            name: "FederalIdentification",
            table: "BallotQuestions");

        migrationBuilder.AddColumn<int>(
            name: "FederalIdentification",
            table: "Ballots",
            type: "integer",
            nullable: true);
    }
}
