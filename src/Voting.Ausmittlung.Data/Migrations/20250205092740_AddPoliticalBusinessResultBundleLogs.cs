// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddPoliticalBusinessResultBundleLogs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MajorityElectionResultBundleLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BundleId = table.Column<Guid>(type: "uuid", nullable: false),
                User_SecureConnectId = table.Column<string>(type: "text", nullable: false),
                User_FirstName = table.Column<string>(type: "text", nullable: false),
                User_LastName = table.Column<string>(type: "text", nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionResultBundleLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionResultBundleLogs_MajorityElectionResultBund~",
                    column: x => x.BundleId,
                    principalTable: "MajorityElectionResultBundles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionResultBundleLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BundleId = table.Column<Guid>(type: "uuid", nullable: false),
                User_SecureConnectId = table.Column<string>(type: "text", nullable: false),
                User_FirstName = table.Column<string>(type: "text", nullable: false),
                User_LastName = table.Column<string>(type: "text", nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionResultBundleLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionResultBundleLogs_ProportionalElectionBu~",
                    column: x => x.BundleId,
                    principalTable: "ProportionalElectionBundles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "VoteResultBundleLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BundleId = table.Column<Guid>(type: "uuid", nullable: false),
                User_SecureConnectId = table.Column<string>(type: "text", nullable: false),
                User_FirstName = table.Column<string>(type: "text", nullable: false),
                User_LastName = table.Column<string>(type: "text", nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VoteResultBundleLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_VoteResultBundleLogs_VoteResultBundles_BundleId",
                    column: x => x.BundleId,
                    principalTable: "VoteResultBundles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionResultBundleLogs_BundleId",
            table: "MajorityElectionResultBundleLogs",
            column: "BundleId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionResultBundleLogs_BundleId",
            table: "ProportionalElectionResultBundleLogs",
            column: "BundleId");

        migrationBuilder.CreateIndex(
            name: "IX_VoteResultBundleLogs_BundleId",
            table: "VoteResultBundleLogs",
            column: "BundleId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MajorityElectionResultBundleLogs");

        migrationBuilder.DropTable(
            name: "ProportionalElectionResultBundleLogs");

        migrationBuilder.DropTable(
            name: "VoteResultBundleLogs");
    }
}
