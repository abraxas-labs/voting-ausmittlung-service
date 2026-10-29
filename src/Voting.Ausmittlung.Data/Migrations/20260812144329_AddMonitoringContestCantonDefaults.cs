// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddMonitoringContestCantonDefaults : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "MonitoringAutoShowPoliticalBusinesses",
            table: "ContestCantonDefaults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "MonitoringUseImprovedUi",
            table: "ContestCantonDefaults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "MonitoringAutoShowPoliticalBusinesses",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "MonitoringUseImprovedUi",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MonitoringAutoShowPoliticalBusinesses",
            table: "ContestCantonDefaults");

        migrationBuilder.DropColumn(
            name: "MonitoringUseImprovedUi",
            table: "ContestCantonDefaults");

        migrationBuilder.DropColumn(
            name: "MonitoringAutoShowPoliticalBusinesses",
            table: "CantonSettings");

        migrationBuilder.DropColumn(
            name: "MonitoringUseImprovedUi",
            table: "CantonSettings");
    }
}
