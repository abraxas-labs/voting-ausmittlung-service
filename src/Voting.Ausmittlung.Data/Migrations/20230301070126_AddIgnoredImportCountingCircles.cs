// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddIgnoredImportCountingCircles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IgnoredImportCountingCircles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleId = table.Column<string>(type: "text", nullable: false),
                CountingCircleDescription = table.Column<string>(type: "text", nullable: false),
                IsTestCountingCircle = table.Column<bool>(type: "boolean", nullable: false),
                ResultImportId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IgnoredImportCountingCircles", x => x.Id);
                table.ForeignKey(
                    name: "FK_IgnoredImportCountingCircles_ResultImports_ResultImportId",
                    column: x => x.ResultImportId,
                    principalTable: "ResultImports",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IgnoredImportCountingCircles_ResultImportId",
            table: "IgnoredImportCountingCircles",
            column: "ResultImportId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "IgnoredImportCountingCircles");
    }
}
