// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddCandidateOrigin : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Origin",
            table: "SecondaryMajorityElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "Origin",
            table: "ProportionalElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "Origin",
            table: "MajorityElectionCandidates",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Origin",
            table: "SecondaryMajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "Origin",
            table: "ProportionalElectionCandidates");

        migrationBuilder.DropColumn(
            name: "Origin",
            table: "MajorityElectionCandidates");
    }
}
