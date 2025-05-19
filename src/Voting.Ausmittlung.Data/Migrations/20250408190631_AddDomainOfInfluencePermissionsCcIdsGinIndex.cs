// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddDomainOfInfluencePermissionsCcIdsGinIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // this gin speeds up jsonp array contains syntax
        // e.g. ARRAY[CountingCircleIds] <@ CcIdColumn
        migrationBuilder.Sql("""
                             CREATE INDEX IX_GIN_Doip_Ccids ON "DomainOfInfluencePermissions" USING GIN ("CountingCircleIds");
                             ANALYZE "DomainOfInfluencePermissions";
                             ANALYZE "SimplePoliticalBusinesses";
                             ANALYZE "SimpleCountingCircleResults";
                             ANALYZE "DomainOfInfluences";
                             """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IX_GIN_Doip_Ccids;");
    }
}
