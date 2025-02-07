// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddSecondaryElectionOnSeparateBallot : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "PrimaryMajorityElectionId",
            table: "MajorityElections",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "CandidateReferenceId",
            table: "MajorityElectionCandidates",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElections_PrimaryMajorityElectionId",
            table: "MajorityElections",
            column: "PrimaryMajorityElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionCandidates_CandidateReferenceId",
            table: "MajorityElectionCandidates",
            column: "CandidateReferenceId");

        migrationBuilder.AddForeignKey(
            name: "FK_MajorityElectionCandidates_MajorityElectionCandidates_Candi~",
            table: "MajorityElectionCandidates",
            column: "CandidateReferenceId",
            principalTable: "MajorityElectionCandidates",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_MajorityElections_MajorityElections_PrimaryMajorityElection~",
            table: "MajorityElections",
            column: "PrimaryMajorityElectionId",
            principalTable: "MajorityElections",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_MajorityElectionCandidates_MajorityElectionCandidates_Candi~",
            table: "MajorityElectionCandidates");

        migrationBuilder.DropForeignKey(
            name: "FK_MajorityElections_MajorityElections_PrimaryMajorityElection~",
            table: "MajorityElections");

        migrationBuilder.DropIndex(
            name: "IX_MajorityElections_PrimaryMajorityElectionId",
            table: "MajorityElections");

        migrationBuilder.DropIndex(
            name: "IX_MajorityElectionCandidates_CandidateReferenceId",
            table: "MajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "PrimaryMajorityElectionId",
            table: "MajorityElections");

        migrationBuilder.DropColumn(
            name: "CandidateReferenceId",
            table: "MajorityElectionCandidates");
    }
}
