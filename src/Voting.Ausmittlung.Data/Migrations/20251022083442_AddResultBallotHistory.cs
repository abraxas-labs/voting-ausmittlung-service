// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddResultBallotHistory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MajorityElectionResultBallotLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                User_SecureConnectId = table.Column<string>(type: "text", nullable: false),
                User_FirstName = table.Column<string>(type: "text", nullable: false),
                User_LastName = table.Column<string>(type: "text", nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionResultBallotLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionResultBallotLogs_MajorityElectionResultBall~",
                    column: x => x.BallotId,
                    principalTable: "MajorityElectionResultBallots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionResultBallotLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                User_SecureConnectId = table.Column<string>(type: "text", nullable: false),
                User_FirstName = table.Column<string>(type: "text", nullable: false),
                User_LastName = table.Column<string>(type: "text", nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionResultBallotLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionResultBallotLogs_ProportionalElectionRe~",
                    column: x => x.BallotId,
                    principalTable: "ProportionalElectionResultBallots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "VoteResultBallotLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                User_SecureConnectId = table.Column<string>(type: "text", nullable: false),
                User_FirstName = table.Column<string>(type: "text", nullable: false),
                User_LastName = table.Column<string>(type: "text", nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VoteResultBallotLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_VoteResultBallotLogs_VoteResultBallots_BallotId",
                    column: x => x.BallotId,
                    principalTable: "VoteResultBallots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionResultBallotLogs_BallotId",
            table: "MajorityElectionResultBallotLogs",
            column: "BallotId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionResultBallotLogs_BallotId",
            table: "ProportionalElectionResultBallotLogs",
            column: "BallotId");

        migrationBuilder.CreateIndex(
            name: "IX_VoteResultBallotLogs_BallotId",
            table: "VoteResultBallotLogs",
            column: "BallotId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MajorityElectionResultBallotLogs");

        migrationBuilder.DropTable(
            name: "ProportionalElectionResultBallotLogs");

        migrationBuilder.DropTable(
            name: "VoteResultBallotLogs");
    }
}
