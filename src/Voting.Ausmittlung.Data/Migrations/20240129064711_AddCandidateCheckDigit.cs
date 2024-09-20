// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddCandidateCheckDigit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "CheckDigit",
            table: "SecondaryMajorityElectionCandidates",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "EnforceCandidateCheckDigitForCountingCircles",
            table: "ProportionalElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "EntryParams_CandidateCheckDigit",
            table: "ProportionalElectionResults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "CheckDigit",
            table: "ProportionalElectionCandidates",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "EnforceCandidateCheckDigitForCountingCircles",
            table: "MajorityElections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "EntryParams_CandidateCheckDigit",
            table: "MajorityElectionResults",
            type: "boolean",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CheckDigit",
            table: "MajorityElectionCandidates",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "CantonDefaults_MajorityElectionUseCandidateCheckDigit",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "CantonDefaults_ProportionalElectionUseCandidateCheckDigit",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "MajorityElectionUseCandidateCheckDigit",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "ProportionalElectionUseCandidateCheckDigit",
            table: "CantonSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CheckDigit",
            table: "SecondaryMajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "EnforceCandidateCheckDigitForCountingCircles",
            table: "ProportionalElections");

        migrationBuilder.DropColumn(
            name: "EntryParams_CandidateCheckDigit",
            table: "ProportionalElectionResults");

        migrationBuilder.DropColumn(
            name: "CheckDigit",
            table: "ProportionalElectionCandidates");

        migrationBuilder.DropColumn(
            name: "EnforceCandidateCheckDigitForCountingCircles",
            table: "MajorityElections");

        migrationBuilder.DropColumn(
            name: "EntryParams_CandidateCheckDigit",
            table: "MajorityElectionResults");

        migrationBuilder.DropColumn(
            name: "CheckDigit",
            table: "MajorityElectionCandidates");

        migrationBuilder.DropColumn(
            name: "CantonDefaults_MajorityElectionUseCandidateCheckDigit",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "CantonDefaults_ProportionalElectionUseCandidateCheckDigit",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "MajorityElectionUseCandidateCheckDigit",
            table: "CantonSettings");

        migrationBuilder.DropColumn(
            name: "ProportionalElectionUseCandidateCheckDigit",
            table: "CantonSettings");
    }
}
