// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddPbIdsToProtocolExport : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid[]>(
            name: "PoliticalBusinessIds",
            table: "ProtocolExports",
            type: "uuid[]",
            nullable: false,
            defaultValue: Array.Empty<Guid>());
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PoliticalBusinessIds",
            table: "ProtocolExports");
    }
}
