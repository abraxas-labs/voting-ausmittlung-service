// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddResultBallotModifiedDuringReview : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "ModifiedDuringReview",
            table: "VoteResultBallots",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "ModifiedDuringReview",
            table: "ProportionalElectionResultBallots",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "ModifiedDuringReview",
            table: "MajorityElectionResultBallots",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ModifiedDuringReview",
            table: "VoteResultBallots");

        migrationBuilder.DropColumn(
            name: "ModifiedDuringReview",
            table: "ProportionalElectionResultBallots");

        migrationBuilder.DropColumn(
            name: "ModifiedDuringReview",
            table: "MajorityElectionResultBallots");
    }
}
