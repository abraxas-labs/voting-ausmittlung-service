// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class RoundVoterParticipation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "CountOfVoters_VoterParticipation",
            table: "BallotResults",
            type: "numeric(20,6)",
            nullable: false,
            oldType: "numeric");

        migrationBuilder.AlterColumn<decimal>(
            name: "CountOfVoters_VoterParticipation",
            table: "ProportionalElectionResults",
            type: "numeric(20,6)",
            nullable: false,
            oldType: "numeric");

        migrationBuilder.AlterColumn<decimal>(
            name: "CountOfVoters_VoterParticipation",
            table: "MajorityElectionResults",
            type: "numeric(20,6)",
            nullable: false,
            oldType: "numeric");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "CountOfVoters_VoterParticipation",
            table: "BallotResults",
            type: "numeric",
            nullable: false,
            oldType: "numeric(20,6)");

        migrationBuilder.AlterColumn<decimal>(
            name: "CountOfVoters_VoterParticipation",
            table: "ProportionalElectionResults",
            type: "numeric",
            nullable: false,
            oldType: "numeric(20,6)");

        migrationBuilder.AlterColumn<decimal>(
            name: "CountOfVoters_VoterParticipation",
            table: "MajorityElectionResults",
            type: "numeric",
            nullable: false,
            oldType: "numeric(20,6)");
    }
}
