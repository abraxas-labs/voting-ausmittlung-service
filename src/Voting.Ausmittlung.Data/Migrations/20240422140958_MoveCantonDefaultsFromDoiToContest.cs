// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class MoveCantonDefaultsFromDoiToContest : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_DomainOfInfluenceCantonDefaultsVotingCardChannels_DomainOfI~",
            table: "DomainOfInfluenceCantonDefaultsVotingCardChannels");

        migrationBuilder.DropIndex(
            name: "IX_DomainOfInfluenceCantonDefaultsVotingCardChannels_DomainOfI~",
            table: "DomainOfInfluenceCantonDefaultsVotingCardChannels");

        migrationBuilder.DropColumn(
            name: "CantonDefaults_CountingMachineEnabled",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "CantonDefaults_MajorityElectionAbsoluteMajorityAlgorithm",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "CantonDefaults_MajorityElectionInvalidVotes",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "CantonDefaults_MajorityElectionUseCandidateCheckDigit",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "CantonDefaults_NewZhFeaturesEnabled",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "CantonDefaults_ProportionalElectionUseCandidateCheckDigit",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "CantonDefaults_ProtocolCountingCircleSortType",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "CantonDefaults_ProtocolDomainOfInfluenceSortType",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "DomainOfInfluenceCantonDefaultsDomainOfInfluenceId",
            table: "DomainOfInfluenceCantonDefaultsVotingCardChannels");

        migrationBuilder.RenameColumn(
            name: "CantonDefaults_SwissAbroadVotingRight",
            table: "DomainOfInfluences",
            newName: "SwissAbroadVotingRight");

        migrationBuilder.AddColumn<Guid>(
            name: "ContestCantonDefaultsId",
            table: "DomainOfInfluenceCantonDefaultsVotingCardChannels",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "CountingMachineEnabled",
            table: "ContestCantonDefaults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "MajorityElectionAbsoluteMajorityAlgorithm",
            table: "ContestCantonDefaults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "MajorityElectionInvalidVotes",
            table: "ContestCantonDefaults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "MajorityElectionUseCandidateCheckDigit",
            table: "ContestCantonDefaults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "NewZhFeaturesEnabled",
            table: "ContestCantonDefaults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "ProportionalElectionUseCandidateCheckDigit",
            table: "ContestCantonDefaults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "ProtocolCountingCircleSortType",
            table: "ContestCantonDefaults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ProtocolDomainOfInfluenceSortType",
            table: "ContestCantonDefaults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCantonDefaultsVotingCardChannels_ContestCa~",
            table: "DomainOfInfluenceCantonDefaultsVotingCardChannels",
            column: "ContestCantonDefaultsId");

        migrationBuilder.AddForeignKey(
            name: "FK_DomainOfInfluenceCantonDefaultsVotingCardChannels_ContestCa~",
            table: "DomainOfInfluenceCantonDefaultsVotingCardChannels",
            column: "ContestCantonDefaultsId",
            principalTable: "ContestCantonDefaults",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_DomainOfInfluenceCantonDefaultsVotingCardChannels_ContestCa~",
            table: "DomainOfInfluenceCantonDefaultsVotingCardChannels");

        migrationBuilder.DropIndex(
            name: "IX_DomainOfInfluenceCantonDefaultsVotingCardChannels_ContestCa~",
            table: "DomainOfInfluenceCantonDefaultsVotingCardChannels");

        migrationBuilder.DropColumn(
            name: "ContestCantonDefaultsId",
            table: "DomainOfInfluenceCantonDefaultsVotingCardChannels");

        migrationBuilder.DropColumn(
            name: "CountingMachineEnabled",
            table: "ContestCantonDefaults");

        migrationBuilder.DropColumn(
            name: "MajorityElectionAbsoluteMajorityAlgorithm",
            table: "ContestCantonDefaults");

        migrationBuilder.DropColumn(
            name: "MajorityElectionInvalidVotes",
            table: "ContestCantonDefaults");

        migrationBuilder.DropColumn(
            name: "MajorityElectionUseCandidateCheckDigit",
            table: "ContestCantonDefaults");

        migrationBuilder.DropColumn(
            name: "NewZhFeaturesEnabled",
            table: "ContestCantonDefaults");

        migrationBuilder.DropColumn(
            name: "ProportionalElectionUseCandidateCheckDigit",
            table: "ContestCantonDefaults");

        migrationBuilder.DropColumn(
            name: "ProtocolCountingCircleSortType",
            table: "ContestCantonDefaults");

        migrationBuilder.DropColumn(
            name: "ProtocolDomainOfInfluenceSortType",
            table: "ContestCantonDefaults");

        migrationBuilder.RenameColumn(
            name: "SwissAbroadVotingRight",
            table: "DomainOfInfluences",
            newName: "CantonDefaults_SwissAbroadVotingRight");

        migrationBuilder.AddColumn<bool>(
            name: "CantonDefaults_CountingMachineEnabled",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "CantonDefaults_MajorityElectionAbsoluteMajorityAlgorithm",
            table: "DomainOfInfluences",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "CantonDefaults_MajorityElectionInvalidVotes",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "CantonDefaults_MajorityElectionUseCandidateCheckDigit",
            table: "DomainOfInfluences",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "CantonDefaults_NewZhFeaturesEnabled",
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

        migrationBuilder.AddColumn<int>(
            name: "CantonDefaults_ProtocolCountingCircleSortType",
            table: "DomainOfInfluences",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CantonDefaults_ProtocolDomainOfInfluenceSortType",
            table: "DomainOfInfluences",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<Guid>(
            name: "DomainOfInfluenceCantonDefaultsDomainOfInfluenceId",
            table: "DomainOfInfluenceCantonDefaultsVotingCardChannels",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCantonDefaultsVotingCardChannels_DomainOfI~",
            table: "DomainOfInfluenceCantonDefaultsVotingCardChannels",
            column: "DomainOfInfluenceCantonDefaultsDomainOfInfluenceId");

        migrationBuilder.AddForeignKey(
            name: "FK_DomainOfInfluenceCantonDefaultsVotingCardChannels_DomainOfI~",
            table: "DomainOfInfluenceCantonDefaultsVotingCardChannels",
            column: "DomainOfInfluenceCantonDefaultsDomainOfInfluenceId",
            principalTable: "DomainOfInfluences",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
