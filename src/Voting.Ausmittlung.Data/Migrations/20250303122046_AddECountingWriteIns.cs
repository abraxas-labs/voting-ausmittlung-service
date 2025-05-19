// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddECountingWriteIns : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "CountOfElectionsWithUnmappedWriteIns",
            table: "SimpleCountingCircleResults",
            newName: "CountOfElectionsWithUnmappedEVotingWriteIns");

        migrationBuilder.RenameColumn(
            name: "CountOfElectionsWithUnmappedWriteIns",
            table: "MajorityElectionResults",
            newName: "CountOfElectionsWithUnmappedEVotingWriteIns");

        migrationBuilder.AddColumn<int>(
            name: "CountOfElectionsWithUnmappedECountingWriteIns",
            table: "SimpleCountingCircleResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ImportType",
            table: "SecondaryMajorityElectionWriteInMappings",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ImportType",
            table: "MajorityElectionWriteInMappings",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "CountOfElectionsWithUnmappedECountingWriteIns",
            table: "MajorityElectionResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<Guid>(
            name: "ImportId",
            table: "SecondaryMajorityElectionWriteInMappings",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<Guid>(
            name: "ImportId",
            table: "MajorityElectionWriteInMappings",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionWriteInMappings_ImportId",
            table: "SecondaryMajorityElectionWriteInMappings",
            column: "ImportId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionWriteInMappings_ImportId",
            table: "MajorityElectionWriteInMappings",
            column: "ImportId");

        migrationBuilder.AddColumn<Guid>(
            name: "ImportId",
            table: "SecondaryMajorityElectionWriteInBallots",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<Guid>(
            name: "ImportId",
            table: "MajorityElectionWriteInBallots",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.CreateIndex(
            name: "IX_SecondaryMajorityElectionWriteInBallots_ImportId",
            table: "SecondaryMajorityElectionWriteInBallots",
            column: "ImportId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionWriteInBallots_ImportId",
            table: "MajorityElectionWriteInBallots",
            column: "ImportId");

        // set import type = EVoting for all existing imports
        migrationBuilder.Sql("""
                             UPDATE "MajorityElectionWriteInMappings"
                             SET "ImportType" = 1
                             """);
        migrationBuilder.Sql("""
                             UPDATE "SecondaryMajorityElectionWriteInMappings"
                             SET "ImportType" = 1
                             """);

        // set ImportIds for existing write in mapping (ballots)
        migrationBuilder.Sql(""""
                             UPDATE "MajorityElectionWriteInMappings" m
                             SET "ImportId" = (
                                 SELECT i."Id"
                                 FROM "ResultImports" i
                                 WHERE i."ImportType" = m."ImportType"
                                 ORDER BY i."Started" DESC
                                 LIMIT 1
                             );
                             """");

        migrationBuilder.Sql(""""
                             UPDATE "SecondaryMajorityElectionWriteInMappings" m
                             SET "ImportId" = (
                                SELECT i."Id"
                                FROM "ResultImports" i
                                WHERE i."ImportType" = m."ImportType"
                                ORDER BY i."Started" DESC
                                LIMIT 1
                             );
                             """");

        migrationBuilder.Sql(""""
                             UPDATE "MajorityElectionWriteInBallots" m
                             SET "ImportId" = (
                                 SELECT i."Id"
                                 FROM "ResultImports" i
                                 ORDER BY i."Started" DESC
                                 LIMIT 1
                             );
                             """");

        migrationBuilder.Sql(""""
                             UPDATE "SecondaryMajorityElectionWriteInBallots" m
                             SET "ImportId" = (
                                SELECT i."Id"
                                FROM "ResultImports" i
                                ORDER BY i."Started" DESC
                                LIMIT 1
                             );
                             """");

        migrationBuilder.AddForeignKey(
            name: "FK_MajorityElectionWriteInBallots_ResultImports_ImportId",
            table: "MajorityElectionWriteInBallots",
            column: "ImportId",
            principalTable: "ResultImports",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_SecondaryMajorityElectionWriteInBallots_ResultImports_Impor~",
            table: "SecondaryMajorityElectionWriteInBallots",
            column: "ImportId",
            principalTable: "ResultImports",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_MajorityElectionWriteInMappings_ResultImports_ImportId",
            table: "MajorityElectionWriteInMappings",
            column: "ImportId",
            principalTable: "ResultImports",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_SecondaryMajorityElectionWriteInMappings_ResultImports_Impo~",
            table: "SecondaryMajorityElectionWriteInMappings",
            column: "ImportId",
            principalTable: "ResultImports",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new InvalidOperationException();
    }
}
