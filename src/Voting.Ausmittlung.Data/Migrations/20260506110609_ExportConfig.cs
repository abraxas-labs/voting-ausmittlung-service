// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class ExportConfig : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ResultExportConfigurations_DomainOfInfluences_DomainOfInflu~",
            table: "ResultExportConfigurations");

        migrationBuilder.DropColumn(
            name: "Description",
            table: "ResultExportConfigurations");

        migrationBuilder.DropColumn(
            name: "EaiMessageType",
            table: "ResultExportConfigurations");

        migrationBuilder.DropColumn(
            name: "ExportKeys",
            table: "ResultExportConfigurations");

        migrationBuilder.DropColumn(
            name: "Provider",
            table: "ResultExportConfigurations");

        migrationBuilder.AlterColumn<Guid>(
            name: "DomainOfInfluenceId",
            table: "ResultExportConfigurations",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.CreateIndex(
            name: "IX_ResultExportConfigurations_ExportConfigurationId",
            table: "ResultExportConfigurations",
            column: "ExportConfigurationId");

        migrationBuilder.AddForeignKey(
            name: "FK_ResultExportConfigurations_DomainOfInfluences_DomainOfInflu~",
            table: "ResultExportConfigurations",
            column: "DomainOfInfluenceId",
            principalTable: "DomainOfInfluences",
            principalColumn: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_ResultExportConfigurations_ExportConfigurations_ExportConfi~",
            table: "ResultExportConfigurations",
            column: "ExportConfigurationId",
            principalTable: "ExportConfigurations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ResultExportConfigurations_DomainOfInfluences_DomainOfInflu~",
            table: "ResultExportConfigurations");

        migrationBuilder.DropForeignKey(
            name: "FK_ResultExportConfigurations_ExportConfigurations_ExportConfi~",
            table: "ResultExportConfigurations");

        migrationBuilder.DropIndex(
            name: "IX_ResultExportConfigurations_ExportConfigurationId",
            table: "ResultExportConfigurations");

        migrationBuilder.AlterColumn<Guid>(
            name: "DomainOfInfluenceId",
            table: "ResultExportConfigurations",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "ResultExportConfigurations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "EaiMessageType",
            table: "ResultExportConfigurations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string[]>(
            name: "ExportKeys",
            table: "ResultExportConfigurations",
            type: "text[]",
            nullable: false,
            defaultValue: new string[0]);

        migrationBuilder.AddColumn<int>(
            name: "Provider",
            table: "ResultExportConfigurations",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddForeignKey(
            name: "FK_ResultExportConfigurations_DomainOfInfluences_DomainOfInflu~",
            table: "ResultExportConfigurations",
            column: "DomainOfInfluenceId",
            principalTable: "DomainOfInfluences",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
