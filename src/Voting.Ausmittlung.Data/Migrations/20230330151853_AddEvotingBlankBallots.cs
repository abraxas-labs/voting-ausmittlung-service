// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddEvotingBlankBallots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "ProportionalElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalBlankBallots",
            table: "ProportionalElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "ProportionalElectionEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalBlankBallots",
            table: "ProportionalElectionEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalBlankBallots",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalBlankBallots",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "BallotResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalBlankBallots",
            table: "BallotResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "BallotEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalBlankBallots",
            table: "BallotEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "ProportionalElectionResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalBlankBallots",
            table: "ProportionalElectionResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "ProportionalElectionEndResult");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalBlankBallots",
            table: "ProportionalElectionEndResult");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "MajorityElectionResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalBlankBallots",
            table: "MajorityElectionResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "MajorityElectionEndResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalBlankBallots",
            table: "MajorityElectionEndResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "BallotResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalBlankBallots",
            table: "BallotResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "BallotEndResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalBlankBallots",
            table: "BallotEndResults");
    }
}
