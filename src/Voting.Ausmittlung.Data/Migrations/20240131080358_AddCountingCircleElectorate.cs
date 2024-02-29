// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddCountingCircleElectorate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ContestCountingCircleElectorates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                DomainOfInfluenceTypes = table.Column<int[]>(type: "integer[]", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContestCountingCircleElectorates", x => x.Id);
                table.ForeignKey(
                    name: "FK_ContestCountingCircleElectorates_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ContestCountingCircleElectorates_CountingCircles_CountingCi~",
                    column: x => x.CountingCircleId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CountingCircleElectorates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CountingCircleId = table.Column<Guid>(type: "uuid", nullable: false),
                DomainOfInfluenceTypes = table.Column<int[]>(type: "integer[]", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CountingCircleElectorates", x => x.Id);
                table.ForeignKey(
                    name: "FK_CountingCircleElectorates_CountingCircles_CountingCircleId",
                    column: x => x.CountingCircleId,
                    principalTable: "CountingCircles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ContestCountingCircleElectorates_ContestId",
            table: "ContestCountingCircleElectorates",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_ContestCountingCircleElectorates_CountingCircleId",
            table: "ContestCountingCircleElectorates",
            column: "CountingCircleId");

        migrationBuilder.CreateIndex(
            name: "IX_CountingCircleElectorates_CountingCircleId",
            table: "CountingCircleElectorates",
            column: "CountingCircleId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ContestCountingCircleElectorates");

        migrationBuilder.DropTable(
            name: "CountingCircleElectorates");
    }
}
