// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class ChangeFederalIdentificationDataType : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "FederalIdentification",
            table: "TieBreakQuestions",
            type: "text",
            nullable: false,
            defaultValue: string.Empty,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "FederalIdentification",
            table: "ProportionalElections",
            type: "text",
            nullable: false,
            defaultValue: string.Empty,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "FederalIdentification",
            table: "MajorityElections",
            type: "text",
            nullable: false,
            defaultValue: string.Empty,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "FederalIdentification",
            table: "BallotQuestions",
            type: "text",
            nullable: false,
            defaultValue: string.Empty,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "FederalIdentification",
            table: "TieBreakQuestions",
            type: "integer",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<int>(
            name: "FederalIdentification",
            table: "ProportionalElections",
            type: "integer",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<int>(
            name: "FederalIdentification",
            table: "MajorityElections",
            type: "integer",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<int>(
            name: "FederalIdentification",
            table: "BallotQuestions",
            type: "integer",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text");
    }
}
