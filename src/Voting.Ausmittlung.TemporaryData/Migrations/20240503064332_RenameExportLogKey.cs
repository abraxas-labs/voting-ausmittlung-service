// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.TemporaryData.Migrations;

/// <inheritdoc />
public partial class RenameExportLogKey : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "TemplateKey",
            table: "ExportLogEntries",
            newName: "ExportKey");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "ExportKey",
            table: "ExportLogEntries",
            newName: "TemplateKey");
    }
}
