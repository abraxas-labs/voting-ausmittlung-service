// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddCountingCircleResultEVotingVotingCardsSent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "TotalSentEVotingVotingCards",
            table: "VoteResults",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "TotalSentEVotingVotingCards",
            table: "ProportionalElectionResults",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "TotalSentEVotingVotingCards",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TotalSentEVotingVotingCards",
            table: "VoteResults");

        migrationBuilder.DropColumn(
            name: "TotalSentEVotingVotingCards",
            table: "ProportionalElectionResults");

        migrationBuilder.DropColumn(
            name: "TotalSentEVotingVotingCards",
            table: "MajorityElectionResults");
    }
}
