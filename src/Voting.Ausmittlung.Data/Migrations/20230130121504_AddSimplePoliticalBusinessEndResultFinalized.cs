// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddSimplePoliticalBusinessEndResultFinalized : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "EndResultFinalized",
            table: "SimplePoliticalBusinesses",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql(@"
                UPDATE ""SimplePoliticalBusinesses""
                SET ""EndResultFinalized"" = true
                FROM (
                    SELECT ""MajorityElectionId"" as ""PoliticalBusinessId"", ""Finalized"" FROM ""MajorityElectionEndResults"" where ""Finalized""
                    UNION ALL
                    SELECT ""ProportionalElectionId"" as ""PoliticalBusinessId"", ""Finalized"" FROM ""ProportionalElectionEndResult"" where ""Finalized""
                    UNION ALL
                    SELECT ""VoteId"" as ""PoliticalBusinessId"", ""Finalized"" FROM ""VoteEndResults"" where ""Finalized""
                ) as data
                WHERE ""SimplePoliticalBusinesses"".""Id"" = data.""PoliticalBusinessId""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EndResultFinalized",
            table: "SimplePoliticalBusinesses");
    }
}
