// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddCandidateResultWriteInsVoteCount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "EVotingVoteCount",
            table: "SecondaryMajorityElectionCandidateResults",
            newName: "EVotingExclWriteInsVoteCount");

        migrationBuilder.RenameColumn(
            name: "EVotingVoteCount",
            table: "MajorityElectionCandidateResults",
            newName: "EVotingExclWriteInsVoteCount");

        migrationBuilder.AddColumn<int>(
            name: "EVotingWriteInsVoteCount",
            table: "SecondaryMajorityElectionCandidateResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "EVotingWriteInsVoteCount",
            table: "MajorityElectionCandidateResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EVotingWriteInsVoteCount",
            table: "SecondaryMajorityElectionCandidateResults");

        migrationBuilder.DropColumn(
            name: "EVotingWriteInsVoteCount",
            table: "MajorityElectionCandidateResults");

        migrationBuilder.RenameColumn(
            name: "EVotingExclWriteInsVoteCount",
            table: "SecondaryMajorityElectionCandidateResults",
            newName: "EVotingVoteCount");

        migrationBuilder.RenameColumn(
            name: "EVotingExclWriteInsVoteCount",
            table: "MajorityElectionCandidateResults",
            newName: "EVotingVoteCount");
    }
}
