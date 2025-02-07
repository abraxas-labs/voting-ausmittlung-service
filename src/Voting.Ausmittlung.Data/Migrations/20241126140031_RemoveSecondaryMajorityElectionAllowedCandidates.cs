// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class RemoveSecondaryMajorityElectionAllowedCandidates : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AllowedCandidates",
            table: "SecondaryMajorityElections");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "AllowedCandidates",
            table: "SecondaryMajorityElections",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }
}
