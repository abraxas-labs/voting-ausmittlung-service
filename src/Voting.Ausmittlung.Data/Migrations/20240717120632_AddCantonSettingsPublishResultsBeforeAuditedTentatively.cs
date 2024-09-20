﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddCantonSettingsPublishResultsBeforeAuditedTentatively : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "PublishResultsBeforeAuditedTentatively",
            table: "ContestCantonDefaults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "PublishResultsBeforeAuditedTentatively",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PublishResultsBeforeAuditedTentatively",
            table: "ContestCantonDefaults");

        migrationBuilder.DropColumn(
            name: "PublishResultsBeforeAuditedTentatively",
            table: "CantonSettings");
    }
}
