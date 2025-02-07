// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddDoiPublishResults : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "PublishResultsEnabled",
            table: "ContestCantonDefaults",
            newName: "ManualPublishResultsEnabled");

        migrationBuilder.RenameColumn(
            name: "PublishResultsEnabled",
            table: "CantonSettings",
            newName: "ManualPublishResultsEnabled");

        migrationBuilder.AddColumn<bool>(
            name: "PublishResultsDisabled",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PublishResultsDisabled",
            table: "DomainOfInfluences");

        migrationBuilder.RenameColumn(
            name: "ManualPublishResultsEnabled",
            table: "ContestCantonDefaults",
            newName: "PublishResultsEnabled");

        migrationBuilder.RenameColumn(
            name: "ManualPublishResultsEnabled",
            table: "CantonSettings",
            newName: "PublishResultsEnabled");
    }
}
