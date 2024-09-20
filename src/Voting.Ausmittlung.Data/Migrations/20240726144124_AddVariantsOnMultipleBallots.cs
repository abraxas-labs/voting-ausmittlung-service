// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddVariantsOnMultipleBallots : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Type",
            table: "Votes",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<int>(
            name: "SubType",
            table: "Ballots",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "BallotTranslations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ShortDescription = table.Column<string>(type: "text", nullable: false),
                OfficialDescription = table.Column<string>(type: "text", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
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

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BallotTranslations");

        migrationBuilder.DropColumn(
            name: "Type",
            table: "Votes");

        migrationBuilder.DropColumn(
            name: "SubType",
            table: "Ballots");
    }
}
