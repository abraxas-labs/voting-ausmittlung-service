// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddIndexForViewCountingCirclePartialResults : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluences_ViewCountingCirclePartialResults",
            table: "DomainOfInfluences",
            column: "ViewCountingCirclePartialResults");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_DomainOfInfluences_ViewCountingCirclePartialResults",
            table: "DomainOfInfluences");
    }
}
