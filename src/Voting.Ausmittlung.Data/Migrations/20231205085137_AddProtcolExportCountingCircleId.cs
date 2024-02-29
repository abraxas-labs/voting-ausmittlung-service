// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddProtcolExportCountingCircleId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "CountingCircleId",
            table: "ProtocolExports",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProtocolExports_CountingCircleId",
            table: "ProtocolExports",
            column: "CountingCircleId");

        migrationBuilder.AddForeignKey(
            name: "FK_ProtocolExports_CountingCircles_CountingCircleId",
            table: "ProtocolExports",
            column: "CountingCircleId",
            principalTable: "CountingCircles",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ProtocolExports_CountingCircles_CountingCircleId",
            table: "ProtocolExports");

        migrationBuilder.DropIndex(
            name: "IX_ProtocolExports_CountingCircleId",
            table: "ProtocolExports");

        migrationBuilder.DropColumn(
            name: "CountingCircleId",
            table: "ProtocolExports");
    }
}
