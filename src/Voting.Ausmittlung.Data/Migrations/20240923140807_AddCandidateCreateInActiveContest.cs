// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddCandidateCreateInActiveContest : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "CreatedDuringActiveContest",
            table: "SecondaryMajorityElectionCandidates",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "CreatedDuringActiveContest",
            table: "MajorityElectionCandidates",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CreatedDuringActiveContest",
            table: "SecondaryMajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "CreatedDuringActiveContest",
            table: "MajorityElectionCandidates");
    }
}
