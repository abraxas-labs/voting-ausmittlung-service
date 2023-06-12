// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddImportedCountingCircles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ResultImportCountingCircle",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ResultImportId = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResultImportCountingCircle", x => x.Id);
                table.ForeignKey(
                    name: "FK_ResultImportCountingCircle_CountingCircles_CountingCircleId",
                    column: x => x.CountingCircleId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ResultImportCountingCircle_ResultImports_ResultImportId",
                    column: x => x.ResultImportId,
                    principalTable: "ResultImports",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ResultImportCountingCircle_CountingCircleId",
            table: "ResultImportCountingCircle",
            column: "CountingCircleId");

        migrationBuilder.CreateIndex(
            name: "IX_ResultImportCountingCircle_ResultImportId",
            table: "ResultImportCountingCircle",
            column: "ResultImportId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ResultImportCountingCircle");
    }
}
