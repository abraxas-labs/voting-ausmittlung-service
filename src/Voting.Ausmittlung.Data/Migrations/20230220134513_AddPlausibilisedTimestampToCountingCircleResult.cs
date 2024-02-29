// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddPlausibilisedTimestampToCountingCircleResult : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "PlausibilisedTimestamp",
            table: "VoteResults",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PlausibilisedTimestamp",
            table: "ProportionalElectionResults",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PlausibilisedTimestamp",
            table: "MajorityElectionResults",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PlausibilisedTimestamp",
            table: "VoteResults");

        migrationBuilder.DropColumn(
            name: "PlausibilisedTimestamp",
            table: "ProportionalElectionResults");

        migrationBuilder.DropColumn(
            name: "PlausibilisedTimestamp",
            table: "MajorityElectionResults");
    }
}
