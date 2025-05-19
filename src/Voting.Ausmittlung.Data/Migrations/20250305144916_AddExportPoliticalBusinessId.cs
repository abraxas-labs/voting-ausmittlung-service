// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddExportPoliticalBusinessId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "PoliticalBusinessId",
            table: "ProtocolExports",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "PoliticalBusinessResultBundleId",
            table: "ProtocolExports",
            type: "uuid",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PoliticalBusinessId",
            table: "ProtocolExports");
        migrationBuilder.DropColumn(
            name: "PoliticalBusinessResultBundleId",
            table: "ProtocolExports");
    }
}
