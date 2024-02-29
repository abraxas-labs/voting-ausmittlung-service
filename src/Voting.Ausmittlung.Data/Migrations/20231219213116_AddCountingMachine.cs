// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddCountingMachine : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "CantonDefaults_CountingMachineEnabled",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "CountingMachine",
            table: "ContestCountingCircleDetails",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "CountingMachineEnabled",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CantonDefaults_CountingMachineEnabled",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "CountingMachine",
            table: "ContestCountingCircleDetails");

        migrationBuilder.DropColumn(
            name: "CountingMachineEnabled",
            table: "CantonSettings");
    }
}
