// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class FixContestDeleteBehavior : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ProportionalElectionCandidates_DomainOfInfluenceParties_Par~",
            table: "ProportionalElectionCandidates");

        migrationBuilder.AddForeignKey(
            name: "FK_ProportionalElectionCandidates_DomainOfInfluenceParties_Par~",
            table: "ProportionalElectionCandidates",
            column: "PartyId",
            principalTable: "DomainOfInfluenceParties",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ProportionalElectionCandidates_DomainOfInfluenceParties_Par~",
            table: "ProportionalElectionCandidates");

        migrationBuilder.AddForeignKey(
            name: "FK_ProportionalElectionCandidates_DomainOfInfluenceParties_Par~",
            table: "ProportionalElectionCandidates",
            column: "PartyId",
            principalTable: "DomainOfInfluenceParties",
            principalColumn: "Id");
    }
}
