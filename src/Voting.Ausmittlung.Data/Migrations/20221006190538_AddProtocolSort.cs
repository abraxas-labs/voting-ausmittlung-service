// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddProtocolSort : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "CantonDefaults_ProtocolCountingCircleSortType",
            table: "DomainOfInfluences",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CantonDefaults_ProtocolDomainOfInfluenceSortType",
            table: "DomainOfInfluences",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "NameForProtocol",
            table: "DomainOfInfluences",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "NameForProtocol",
            table: "CountingCircles",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<int>(
            name: "SortNumber",
            table: "CountingCircles",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ProtocolCountingCircleSortType",
            table: "CantonSettings",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ProtocolDomainOfInfluenceSortType",
            table: "CantonSettings",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CantonDefaults_ProtocolCountingCircleSortType",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "CantonDefaults_ProtocolDomainOfInfluenceSortType",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "NameForProtocol",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "NameForProtocol",
            table: "CountingCircles");

        migrationBuilder.DropColumn(
            name: "SortNumber",
            table: "CountingCircles");

        migrationBuilder.DropColumn(
            name: "ProtocolCountingCircleSortType",
            table: "CantonSettings");

        migrationBuilder.DropColumn(
            name: "ProtocolDomainOfInfluenceSortType",
            table: "CantonSettings");
    }
}
