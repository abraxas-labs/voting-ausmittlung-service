// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class RemoveBallotDescription : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BallotTranslations");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BallotTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Language = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BallotTranslations", x => x.Id);
                table.ForeignKey(
                    name: "FK_BallotTranslations_Ballots_BallotId",
                    column: x => x.BallotId,
                    principalTable: "Ballots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BallotTranslations_BallotId_Language",
            table: "BallotTranslations",
            columns: new[] { "BallotId", "Language" },
            unique: true);
    }
}
