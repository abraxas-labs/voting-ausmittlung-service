// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddPoliticalBusinessSubType : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "PoliticalBusinessSubType",
            table: "SimplePoliticalBusinesses",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql("""
            UPDATE "SimplePoliticalBusinesses" AS spb SET "PoliticalBusinessSubType" = 1
            WHERE "PoliticalBusinessType" = 1 AND EXISTS (
                SELECT 1 FROM "Votes" v INNER JOIN "Ballots" b ON v."Id" = b."VoteId"
                WHERE v."Id" = spb."Id" AND (v."Type" = 2 OR b."BallotType" = 2)
            )
        """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PoliticalBusinessSubType",
            table: "SimplePoliticalBusinesses");
    }
}
