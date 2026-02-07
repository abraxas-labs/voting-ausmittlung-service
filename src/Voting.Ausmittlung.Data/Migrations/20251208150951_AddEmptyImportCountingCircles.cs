// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddEmptyImportCountingCircles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EmptyImportCountingCircle",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                ResultImportId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmptyImportCountingCircle", x => x.Id);
                table.ForeignKey(
                    name: "FK_EmptyImportCountingCircle_CountingCircles_CountingCircleId",
                    column: x => x.CountingCircleId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_EmptyImportCountingCircle_ResultImports_ResultImportId",
                    column: x => x.ResultImportId,
                    principalTable: "ResultImports",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EmptyImportCountingCircle_CountingCircleId",
            table: "EmptyImportCountingCircle",
            column: "CountingCircleId");

        migrationBuilder.CreateIndex(
            name: "IX_EmptyImportCountingCircle_ResultImportId",
            table: "EmptyImportCountingCircle",
            column: "ResultImportId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "EmptyImportCountingCircle");
    }
}
