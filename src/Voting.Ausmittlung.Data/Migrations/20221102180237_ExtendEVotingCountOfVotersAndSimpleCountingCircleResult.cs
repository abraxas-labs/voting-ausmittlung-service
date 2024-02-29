// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class ExtendEVotingCountOfVotersAndSimpleCountingCircleResult : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "CountOfElectionsWithUnmappedWriteIns",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "ProportionalElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "ProportionalElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalInvalidBallots",
            table: "ProportionalElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "ProportionalElectionEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "ProportionalElectionEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalInvalidBallots",
            table: "ProportionalElectionEndResult",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalInvalidBallots",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalInvalidBallots",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "BallotResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "BallotResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalInvalidBallots",
            table: "BallotResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "BallotEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "BallotEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalInvalidBallots",
            table: "BallotEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CountOfElectionsWithUnmappedWriteIns",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "ProportionalElectionResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "ProportionalElectionResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalInvalidBallots",
            table: "ProportionalElectionResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "ProportionalElectionEndResult");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "ProportionalElectionEndResult");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalInvalidBallots",
            table: "ProportionalElectionEndResult");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "MajorityElectionResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "MajorityElectionResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalInvalidBallots",
            table: "MajorityElectionResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "MajorityElectionEndResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "MajorityElectionEndResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalInvalidBallots",
            table: "MajorityElectionEndResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "BallotResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "BallotResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalInvalidBallots",
            table: "BallotResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "BallotEndResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "BallotEndResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalInvalidBallots",
            table: "BallotEndResults");
    }
}
