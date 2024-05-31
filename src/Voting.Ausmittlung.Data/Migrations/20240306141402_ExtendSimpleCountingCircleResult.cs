// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class ExtendSimpleCountingCircleResult : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "AuditedTentativelyTimestamp",
            table: "SimpleCountingCircleResults",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ConventionalAccountedBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ConventionalBlankBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ConventionalInvalidBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_ConventionalReceivedBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_EVotingReceivedBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalAccountedBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalBlankBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalInvalidBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalReceivedBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfVoters_TotalUnaccountedBallots",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<decimal>(
            name: "CountOfVoters_VoterParticipation",
            table: "SimpleCountingCircleResults",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<DateTime>(
            name: "PlausibilisedTimestamp",
            table: "SimpleCountingCircleResults",
            type: "timestamp with time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AuditedTentativelyTimestamp",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_ConventionalAccountedBallots",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_ConventionalBlankBallots",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_ConventionalInvalidBallots",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_ConventionalReceivedBallots",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingAccountedBallots",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingBlankBallots",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingInvalidBallots",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_EVotingReceivedBallots",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalAccountedBallots",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalBlankBallots",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalInvalidBallots",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalReceivedBallots",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_TotalUnaccountedBallots",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "CountOfVoters_VoterParticipation",
            table: "SimpleCountingCircleResults");

        migrationBuilder.DropColumn(
            name: "PlausibilisedTimestamp",
            table: "SimpleCountingCircleResults");
    }
}
