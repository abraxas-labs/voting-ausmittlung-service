// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddSimpleCcResultIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // EF Core detects that a new index will be created with the PoliticalBusinessId in it.
        // It does not make sense to have a nearly duplicated index for the PoliticalBusinessId, as the
        // newly created index can also be used for this purpose.
        migrationBuilder.DropIndex(
            name: "IX_SimpleCountingCircleResults_PoliticalBusinessId",
            table: "SimpleCountingCircleResults");

        migrationBuilder.CreateIndex(
            name: "IX_SimpleCountingCircleResults_PoliticalBusinessId_CountingCir~",
            table: "SimpleCountingCircleResults",
            columns: new[] { "PoliticalBusinessId", "CountingCircleId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SimpleCountingCircleResults_PoliticalBusinessId_CountingCir~",
            table: "SimpleCountingCircleResults");

        migrationBuilder.CreateIndex(
            name: "IX_SimpleCountingCircleResults_PoliticalBusinessId",
            table: "SimpleCountingCircleResults",
            column: "PoliticalBusinessId");
    }
}
