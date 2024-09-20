// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddMajorityElectionWriteInBallots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MajorityElectionWriteInBallots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ResultId = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                EmptyVoteCount = table.Column<int>(type: "integer", nullable: false),
                InvalidVoteCount = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionWriteInBallots", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionWriteInBallots_MajorityElectionResults_Resu~",
                    column: x => x.ResultId,
                    principalTable: "MajorityElectionResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SecondaryMajorityElectionWriteInBallots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ResultId = table.Column<Guid>(type: "uuid", nullable: false),
                CandidateIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                EmptyVoteCount = table.Column<int>(type: "integer", nullable: false),
                InvalidVoteCount = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecondaryMajorityElectionWriteInBallots", x => x.Id);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionWriteInBallots_SecondaryMajorityEl~",
                    column: x => x.ResultId,
                    principalTable: "SecondaryMajorityElectionResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionWriteInBallotPositions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                WriteInMappingId = table.Column<Guid>(type: "uuid", nullable: false),
                Target = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionWriteInBallotPositions", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionWriteInBallotPositions_MajorityElectionWri~1",
                    column: x => x.WriteInMappingId,
                    principalTable: "MajorityElectionWriteInMappings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MajorityElectionWriteInBallotPositions_MajorityElectionWrit~",
                    column: x => x.BallotId,
                    principalTable: "MajorityElectionWriteInBallots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SecondaryMajorityElectionWriteInBallotPositions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                WriteInMappingId = table.Column<Guid>(type: "uuid", nullable: false),
                Target = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecondaryMajorityElectionWriteInBallotPositions", x => x.Id);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionWriteInBallotPositions_SecondaryM~1",
                    column: x => x.WriteInMappingId,
                    principalTable: "SecondaryMajorityElectionWriteInMappings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SecondaryMajorityElectionWriteInBallotPositions_SecondaryMa~",
                    column: x => x.BallotId,
                    principalTable: "SecondaryMajorityElectionWriteInBallots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionWriteInBallotPositions_BallotId",
            table: "MajorityElectionWriteInBallotPositions",
            column: "BallotId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionWriteInBallotPositions_WriteInMappingId",
            table: "MajorityElectionWriteInBallotPositions",
            column: "WriteInMappingId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionWriteInBallots_ResultId",
            table: "MajorityElectionWriteInBallots",
            column: "ResultId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionWriteInBallotPositions_BallotId",
            table: "SecondaryMajorityElectionWriteInBallotPositions",
            column: "BallotId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionWriteInBallotPositions_WriteInMapp~",
            table: "SecondaryMajorityElectionWriteInBallotPositions",
            column: "WriteInMappingId");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionWriteInBallots_ResultId",
            table: "SecondaryMajorityElectionWriteInBallots",
            column: "ResultId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MajorityElectionWriteInBallotPositions");

        migrationBuilder.DropTable(
            name: "SecondaryMajorityElectionWriteInBallotPositions");

        migrationBuilder.DropTable(
            name: "MajorityElectionWriteInBallots");

        migrationBuilder.DropTable(
            name: "SecondaryMajorityElectionWriteInBallots");
    }
}
