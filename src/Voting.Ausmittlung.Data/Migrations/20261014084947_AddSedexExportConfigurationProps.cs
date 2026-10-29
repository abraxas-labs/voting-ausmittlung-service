// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddSedexExportConfigurationProps : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string[]>(
            name: "SedexRecipientIds",
            table: "ExportConfigurations",
            type: "text[]",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SedexSenderId",
            table: "ExportConfigurations",
            type: "text",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SedexRecipientIds",
            table: "ExportConfigurations");

        migrationBuilder.DropColumn(
            name: "SedexSenderId",
            table: "ExportConfigurations");
    }
}
