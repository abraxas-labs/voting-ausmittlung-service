// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddNullableDateOfBirthForMajorityElectionCandidates : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            name: "DateOfBirth",
            table: "SecondaryMajorityElectionCandidates",
            type: "date",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "date");

        migrationBuilder.AlterColumn<DateTime>(
            name: "DateOfBirth",
            table: "MajorityElectionCandidates",
            type: "date",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "date");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            name: "DateOfBirth",
            table: "SecondaryMajorityElectionCandidates",
            type: "date",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
            oldClrType: typeof(DateTime),
            oldType: "date",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "DateOfBirth",
            table: "MajorityElectionCandidates",
            type: "date",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
            oldClrType: typeof(DateTime),
            oldType: "date",
            oldNullable: true);
    }
}
