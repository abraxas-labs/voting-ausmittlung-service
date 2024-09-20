// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddReadyForCorrectionTimestamp : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "ReadyForCorrectionTimestamp",
            table: "VoteResults",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ReadyForCorrectionTimestamp",
            table: "SimpleCountingCircleResults",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ReadyForCorrectionTimestamp",
            table: "ProportionalElectionResults",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ReadyForCorrectionTimestamp",
            table: "MajorityElectionResults",
            type: "timestamp with time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ReadyForCorrectionTimestamp",
            table: "VoteResults");

        migrationBuilder.DropColumn(
            name: "ReadyForCorrectionTimestamp",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "ReadyForCorrectionTimestamp",
            table: "ProportionalElectionResults");

        migrationBuilder.DropColumn(
            name: "ReadyForCorrectionTimestamp",
            table: "MajorityElectionResults");
    }
}
