// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddBundleWithBallotGroupOption : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SecondaryMajorityElectionResultBallotCandidates_BallotId_Ca~",
            table: "SecondaryMajorityElectionResultBallotCandidates");

        migrationBuilder.DropIndex(
            name: "IX_MajorityElectionResultBallotCandidates_BallotId_CandidateId",
            table: "MajorityElectionResultBallotCandidates");

        migrationBuilder.AddColumn<bool>(
            name: "OnBallotGroup",
            table: "SecondaryMajorityElectionResultBallotCandidates",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "Position",
            table: "SecondaryMajorityElectionResultBallotCandidates",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "EntryParams_BundlesWithBallotGroupEnabled",
            table: "MajorityElectionResults",
            type: "boolean",
            nullable: true,
            defaultValue: false);

        migrationBuilder.AddColumn<Guid>(
            name: "BallotGroupId",
            table: "MajorityElectionResultBundles",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "OnBallotGroup",
            table: "MajorityElectionResultBallotCandidates",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "Position",
            table: "MajorityElectionResultBallotCandidates",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionResultBallotCandidates_BallotId_Ca~",
            table: "SecondaryMajorityElectionResultBallotCandidates",
            columns: new[] { "BallotId", "CandidateId", "Selected" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionResultBundles_BallotGroupId",
            table: "MajorityElectionResultBundles",
            column: "BallotGroupId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionResultBallotCandidates_BallotId_CandidateId~",
            table: "MajorityElectionResultBallotCandidates",
            columns: new[] { "BallotId", "CandidateId", "Selected" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_MajorityElectionResultBundles_MajorityElectionBallotGroups_~",
            table: "MajorityElectionResultBundles",
            column: "BallotGroupId",
            principalTable: "MajorityElectionBallotGroups",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_MajorityElectionResultBundles_MajorityElectionBallotGroups_~",
            table: "MajorityElectionResultBundles");

        migrationBuilder.DropIndex(
            name: "IX_SecondaryMajorityElectionResultBallotCandidates_BallotId_Ca~",
            table: "SecondaryMajorityElectionResultBallotCandidates");

        migrationBuilder.DropIndex(
            name: "IX_MajorityElectionResultBundles_BallotGroupId",
            table: "MajorityElectionResultBundles");

        migrationBuilder.DropIndex(
            name: "IX_MajorityElectionResultBallotCandidates_BallotId_CandidateId~",
            table: "MajorityElectionResultBallotCandidates");

        migrationBuilder.DropColumn(
            name: "OnBallotGroup",
            table: "SecondaryMajorityElectionResultBallotCandidates");

        migrationBuilder.DropColumn(
            name: "Position",
            table: "SecondaryMajorityElectionResultBallotCandidates");

        migrationBuilder.DropColumn(
            name: "EntryParams_BundlesWithBallotGroupEnabled",
            table: "MajorityElectionResults");

        migrationBuilder.DropColumn(
            name: "BallotGroupId",
            table: "MajorityElectionResultBundles");

        migrationBuilder.DropColumn(
            name: "OnBallotGroup",
            table: "MajorityElectionResultBallotCandidates");

        migrationBuilder.DropColumn(
            name: "Position",
            table: "MajorityElectionResultBallotCandidates");

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionResultBallotCandidates_BallotId_Ca~",
            table: "SecondaryMajorityElectionResultBallotCandidates",
            columns: new[] { "BallotId", "CandidateId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionResultBallotCandidates_BallotId_CandidateId",
            table: "MajorityElectionResultBallotCandidates",
            columns: new[] { "BallotId", "CandidateId" },
            unique: true);
    }
}
