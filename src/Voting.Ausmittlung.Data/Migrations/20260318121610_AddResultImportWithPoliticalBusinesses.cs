// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddResultImportWithPoliticalBusinesses : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "ECountingImported",
            table: "SimpleCountingCircleResults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "IgnoredImportPoliticalBusiness",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PoliticalBusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                ResultImportId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IgnoredImportPoliticalBusiness", x => x.Id);
                table.ForeignKey(
                    name: "FK_IgnoredImportPoliticalBusiness_ResultImports_ResultImportId",
                    column: x => x.ResultImportId,
                    principalTable: "ResultImports",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_IgnoredImportPoliticalBusiness_SimplePoliticalBusinesses_Po~",
                    column: x => x.PoliticalBusinessId,
                    principalTable: "SimplePoliticalBusinesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ResultImportPoliticalBusiness",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PoliticalBusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                ResultImportId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResultImportPoliticalBusiness", x => x.Id);
                table.ForeignKey(
                    name: "FK_ResultImportPoliticalBusiness_ResultImports_ResultImportId",
                    column: x => x.ResultImportId,
                    principalTable: "ResultImports",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ResultImportPoliticalBusiness_SimplePoliticalBusinesses_Pol~",
                    column: x => x.PoliticalBusinessId,
                    principalTable: "SimplePoliticalBusinesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IgnoredImportPoliticalBusiness_PoliticalBusinessId",
            table: "IgnoredImportPoliticalBusiness",
            column: "PoliticalBusinessId");

        migrationBuilder.CreateIndex(
            name: "IX_IgnoredImportPoliticalBusiness_ResultImportId",
            table: "IgnoredImportPoliticalBusiness",
            column: "ResultImportId");

        migrationBuilder.CreateIndex(
            name: "IX_ResultImportPoliticalBusiness_PoliticalBusinessId",
            table: "ResultImportPoliticalBusiness",
            column: "PoliticalBusinessId");

        migrationBuilder.CreateIndex(
            name: "IX_ResultImportPoliticalBusiness_ResultImportId",
            table: "ResultImportPoliticalBusiness",
            column: "ResultImportId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "IgnoredImportPoliticalBusiness");

        migrationBuilder.DropTable(
            name: "ResultImportPoliticalBusiness");

        migrationBuilder.DropColumn(
            name: "ECountingImported",
            table: "SimpleCountingCircleResults");
    }
}
