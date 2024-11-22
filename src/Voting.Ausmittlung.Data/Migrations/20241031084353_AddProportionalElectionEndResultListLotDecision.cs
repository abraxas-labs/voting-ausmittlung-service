// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddProportionalElectionEndResultListLotDecision : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProportionalElectionEndResultListLotDecision",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionEndResultId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionEndResultListLotDecision", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionEndResultListLotDecision_ProportionalEl~",
                    column: x => x.ProportionalElectionEndResultId,
                    principalTable: "ProportionalElectionEndResult",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionEndResultListLotDecisionEntry",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionEndResultListLotDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                ListId = table.Column<Guid>(type: "uuid", nullable: true),
                ListUnionId = table.Column<Guid>(type: "uuid", nullable: true),
                Winning = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionEndResultListLotDecisionEntry", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionEndResultListLotDecisionEntry_Proportio~",
                    column: x => x.ListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_ProportionalElectionEndResultListLotDecisionEntry_Proporti~1",
                    column: x => x.ListUnionId,
                    principalTable: "ProportionalElectionListUnions",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_ProportionalElectionEndResultListLotDecisionEntry_Proporti~2",
                    column: x => x.ProportionalElectionEndResultListLotDecisionId,
                    principalTable: "ProportionalElectionEndResultListLotDecision",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionEndResultListLotDecision_ProportionalEl~",
            table: "ProportionalElectionEndResultListLotDecision",
            column: "ProportionalElectionEndResultId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionEndResultListLotDecisionEntry_ListId",
            table: "ProportionalElectionEndResultListLotDecisionEntry",
            column: "ListId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionEndResultListLotDecisionEntry_ListUnion~",
            table: "ProportionalElectionEndResultListLotDecisionEntry",
            column: "ListUnionId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionEndResultListLotDecisionEntry_Proportio~",
            table: "ProportionalElectionEndResultListLotDecisionEntry",
            column: "ProportionalElectionEndResultListLotDecisionId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ProportionalElectionEndResultListLotDecisionEntry");

        migrationBuilder.DropTable(
            name: "ProportionalElectionEndResultListLotDecision");
    }
}
