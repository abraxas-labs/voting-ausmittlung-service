// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddExportConfigurationMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Provider",
            table: "ResultExportConfigurations",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "Provider",
            table: "ExportConfigurations",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "ResultExportConfigurationPoliticalBusinessMetadata",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PoliticalBusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                ResultExportConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                Token = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResultExportConfigurationPoliticalBusinessMetadata", x => x.Id);
                table.ForeignKey(
                    name: "FK_ResultExportConfigurationPoliticalBusinessMetadata_ResultEx~",
                    column: x => x.ResultExportConfigurationId,
                    principalTable: "ResultExportConfigurations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ResultExportConfigurationPoliticalBusinessMetadata_SimplePo~",
                    column: x => x.PoliticalBusinessId,
                    principalTable: "SimplePoliticalBusinesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ResultExportConfigurationPoliticalBusinessMetadata_Politica~",
            table: "ResultExportConfigurationPoliticalBusinessMetadata",
            columns: new[] { "PoliticalBusinessId", "ResultExportConfigurationId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ResultExportConfigurationPoliticalBusinessMetadata_ResultEx~",
            table: "ResultExportConfigurationPoliticalBusinessMetadata",
            column: "ResultExportConfigurationId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ResultExportConfigurationPoliticalBusinessMetadata");

        migrationBuilder.DropColumn(
            name: "Provider",
            table: "ResultExportConfigurations");

        migrationBuilder.DropColumn(
            name: "Provider",
            table: "ExportConfigurations");
    }
}
