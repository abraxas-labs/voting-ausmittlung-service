// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddEmptyWriteInsVoteCount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "EVotingSubTotal_EmptyVoteCount",
            table: "SecondaryMajorityElectionResults",
            newName: "EVotingSubTotal_EmptyVoteCountExclWriteIns");

        migrationBuilder.RenameColumn(
            name: "ConventionalSubTotal_EmptyVoteCount",
            table: "SecondaryMajorityElectionResults",
            newName: "ConventionalSubTotal_EmptyVoteCountExclWriteIns");

        migrationBuilder.RenameColumn(
            name: "EVotingSubTotal_EmptyVoteCount",
            table: "SecondaryMajorityElectionEndResults",
            newName: "EVotingSubTotal_EmptyVoteCountExclWriteIns");

        migrationBuilder.RenameColumn(
            name: "ConventionalSubTotal_EmptyVoteCount",
            table: "SecondaryMajorityElectionEndResults",
            newName: "ConventionalSubTotal_EmptyVoteCountExclWriteIns");

        migrationBuilder.RenameColumn(
            name: "EVotingSubTotal_EmptyVoteCount",
            table: "MajorityElectionResults",
            newName: "EVotingSubTotal_EmptyVoteCountExclWriteIns");

        migrationBuilder.RenameColumn(
            name: "ConventionalSubTotal_EmptyVoteCount",
            table: "MajorityElectionResults",
            newName: "ConventionalSubTotal_EmptyVoteCountExclWriteIns");

        migrationBuilder.RenameColumn(
            name: "EVotingSubTotal_EmptyVoteCount",
            table: "MajorityElectionEndResults",
            newName: "EVotingSubTotal_EmptyVoteCountExclWriteIns");

        migrationBuilder.RenameColumn(
            name: "ConventionalSubTotal_EmptyVoteCount",
            table: "MajorityElectionEndResults",
            newName: "ConventionalSubTotal_EmptyVoteCountExclWriteIns");

        migrationBuilder.AddColumn<int>(
            name: "ConventionalSubTotal_EmptyVoteCountWriteIns",
            table: "SecondaryMajorityElectionResults",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "EVotingSubTotal_EmptyVoteCountWriteIns",
            table: "SecondaryMajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ConventionalSubTotal_EmptyVoteCountWriteIns",
            table: "SecondaryMajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "EVotingSubTotal_EmptyVoteCountWriteIns",
            table: "SecondaryMajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ConventionalSubTotal_EmptyVoteCountWriteIns",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "EVotingSubTotal_EmptyVoteCountWriteIns",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ConventionalSubTotal_EmptyVoteCountWriteIns",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "EVotingSubTotal_EmptyVoteCountWriteIns",
            table: "MajorityElectionEndResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ConventionalSubTotal_EmptyVoteCountWriteIns",
            table: "SecondaryMajorityElectionResults");

        migrationBuilder.DropColumn(
            name: "EVotingSubTotal_EmptyVoteCountWriteIns",
            table: "SecondaryMajorityElectionResults");

        migrationBuilder.DropColumn(
            name: "ConventionalSubTotal_EmptyVoteCountWriteIns",
            table: "SecondaryMajorityElectionEndResults");

        migrationBuilder.DropColumn(
            name: "EVotingSubTotal_EmptyVoteCountWriteIns",
            table: "SecondaryMajorityElectionEndResults");

        migrationBuilder.DropColumn(
            name: "ConventionalSubTotal_EmptyVoteCountWriteIns",
            table: "MajorityElectionResults");

        migrationBuilder.DropColumn(
            name: "EVotingSubTotal_EmptyVoteCountWriteIns",
            table: "MajorityElectionResults");

        migrationBuilder.DropColumn(
            name: "ConventionalSubTotal_EmptyVoteCountWriteIns",
            table: "MajorityElectionEndResults");

        migrationBuilder.DropColumn(
            name: "EVotingSubTotal_EmptyVoteCountWriteIns",
            table: "MajorityElectionEndResults");

        migrationBuilder.RenameColumn(
            name: "EVotingSubTotal_EmptyVoteCountExclWriteIns",
            table: "SecondaryMajorityElectionResults",
            newName: "EVotingSubTotal_EmptyVoteCount");

        migrationBuilder.RenameColumn(
            name: "ConventionalSubTotal_EmptyVoteCountExclWriteIns",
            table: "SecondaryMajorityElectionResults",
            newName: "ConventionalSubTotal_EmptyVoteCount");

        migrationBuilder.RenameColumn(
            name: "EVotingSubTotal_EmptyVoteCountExclWriteIns",
            table: "SecondaryMajorityElectionEndResults",
            newName: "EVotingSubTotal_EmptyVoteCount");

        migrationBuilder.RenameColumn(
            name: "ConventionalSubTotal_EmptyVoteCountExclWriteIns",
            table: "SecondaryMajorityElectionEndResults",
            newName: "ConventionalSubTotal_EmptyVoteCount");

        migrationBuilder.RenameColumn(
            name: "EVotingSubTotal_EmptyVoteCountExclWriteIns",
            table: "MajorityElectionResults",
            newName: "EVotingSubTotal_EmptyVoteCount");

        migrationBuilder.RenameColumn(
            name: "ConventionalSubTotal_EmptyVoteCountExclWriteIns",
            table: "MajorityElectionResults",
            newName: "ConventionalSubTotal_EmptyVoteCount");

        migrationBuilder.RenameColumn(
            name: "EVotingSubTotal_EmptyVoteCountExclWriteIns",
            table: "MajorityElectionEndResults",
            newName: "EVotingSubTotal_EmptyVoteCount");

        migrationBuilder.RenameColumn(
            name: "ConventionalSubTotal_EmptyVoteCountExclWriteIns",
            table: "MajorityElectionEndResults",
            newName: "ConventionalSubTotal_EmptyVoteCount");
    }
}
