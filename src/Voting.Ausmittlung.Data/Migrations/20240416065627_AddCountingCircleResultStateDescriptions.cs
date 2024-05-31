// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddCountingCircleResultStateDescriptions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ContestCantonDefaults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContestCantonDefaults", x => x.Id);
                table.ForeignKey(
                    name: "FK_ContestCantonDefaults_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CountingCircleResultStateDescriptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CantonSettingsId = table.Column<Guid>(type: "uuid", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CountingCircleResultStateDescriptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_CountingCircleResultStateDescriptions_CantonSettings_Canton~",
                    column: x => x.CantonSettingsId,
                    principalTable: "CantonSettings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ContestCantonDefaultsCountingCircleResultStateDescriptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                ContestCantonDefaultsId = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContestCantonDefaultsCountingCircleResultStateDescriptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ContestCantonDefaultsCountingCircleResultStateDescriptions_~",
                    column: x => x.ContestCantonDefaultsId,
                    principalTable: "ContestCantonDefaults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ContestCantonDefaults_ContestId",
            table: "ContestCantonDefaults",
            column: "ContestId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ContestCantonDefaultsCountingCircleResultStateDescriptions_~",
            table: "ContestCantonDefaultsCountingCircleResultStateDescriptions",
            column: "ContestCantonDefaultsId");

        migrationBuilder.CreateIndex(
            name: "IX_CountingCircleResultStateDescriptions_CantonSettingsId_State",
            table: "CountingCircleResultStateDescriptions",
            columns: new[] { "CantonSettingsId", "State" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ContestCantonDefaultsCountingCircleResultStateDescriptions");

        migrationBuilder.DropTable(
            name: "CountingCircleResultStateDescriptions");

        migrationBuilder.DropTable(
            name: "ContestCantonDefaults");
    }
}
