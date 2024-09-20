// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddDoubleProportionalResult : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DoubleProportionalResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionUnionId = table.Column<Guid>(type: "uuid", nullable: true),
                ProportionalElectionId = table.Column<Guid>(type: "uuid", nullable: true),
                NumberOfMandates = table.Column<int>(type: "integer", nullable: false),
                VoteCount = table.Column<int>(type: "integer", nullable: false),
                CantonalQuorum = table.Column<int>(type: "integer", nullable: false),
                VoterNumber = table.Column<decimal>(type: "numeric", nullable: false),
                ElectionKey = table.Column<int>(type: "integer", nullable: false),
                AllNumberOfMandatesDistributed = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DoubleProportionalResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_DoubleProportionalResults_ProportionalElectionUnions_Propor~",
                    column: x => x.ProportionalElectionUnionId,
                    principalTable: "ProportionalElectionUnions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DoubleProportionalResults_ProportionalElections_Proportiona~",
                    column: x => x.ProportionalElectionId,
                    principalTable: "ProportionalElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionUnionEndResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionUnionId = table.Column<Guid>(type: "uuid", nullable: false),
                CountOfDoneElections = table.Column<int>(type: "integer", nullable: false),
                TotalCountOfElections = table.Column<int>(type: "integer", nullable: false),
                Finalized = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionUnionEndResults", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionUnionEndResults_ProportionalElectionUni~",
                    column: x => x.ProportionalElectionUnionId,
                    principalTable: "ProportionalElectionUnions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DoubleProportionalResultColumns",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UnionListId = table.Column<Guid>(type: "uuid", nullable: true),
                ListId = table.Column<Guid>(type: "uuid", nullable: true),
                VoteCount = table.Column<int>(type: "integer", nullable: false),
                CantonalQuorumReached = table.Column<bool>(type: "boolean", nullable: false),
                AnyQuorumReached = table.Column<bool>(type: "boolean", nullable: false),
                VoterNumber = table.Column<decimal>(type: "numeric", nullable: false),
                SuperApportionmentNumberOfMandatesUnrounded = table.Column<decimal>(type: "numeric", nullable: false),
                SuperApportionmentNumberOfMandates = table.Column<int>(type: "integer", nullable: false),
                SubApportionmentNumberOfMandates = table.Column<int>(type: "integer", nullable: false),
                Divisor = table.Column<decimal>(type: "numeric", nullable: false),
                ResultId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DoubleProportionalResultColumns", x => x.Id);
                table.ForeignKey(
                    name: "FK_DoubleProportionalResultColumns_DoubleProportionalResults_R~",
                    column: x => x.ResultId,
                    principalTable: "DoubleProportionalResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DoubleProportionalResultColumns_ProportionalElectionLists_L~",
                    column: x => x.ListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DoubleProportionalResultColumns_ProportionalElectionUnionLi~",
                    column: x => x.UnionListId,
                    principalTable: "ProportionalElectionUnionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DoubleProportionalResultRows",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                VoteCount = table.Column<int>(type: "integer", nullable: false),
                Quorum = table.Column<int>(type: "integer", nullable: false),
                Divisor = table.Column<decimal>(type: "numeric", nullable: false),
                NumberOfMandates = table.Column<int>(type: "integer", nullable: false),
                SubApportionmentNumberOfMandates = table.Column<int>(type: "integer", nullable: false),
                ResultId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DoubleProportionalResultRows", x => x.Id);
                table.ForeignKey(
                    name: "FK_DoubleProportionalResultRows_DoubleProportionalResults_Resu~",
                    column: x => x.ResultId,
                    principalTable: "DoubleProportionalResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DoubleProportionalResultRows_ProportionalElections_Proporti~",
                    column: x => x.ProportionalElectionId,
                    principalTable: "ProportionalElections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DoubleProportionalResultCells",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ListId = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionQuorumReached = table.Column<bool>(type: "boolean", nullable: false),
                VoteCount = table.Column<int>(type: "integer", nullable: false),
                VoterNumber = table.Column<decimal>(type: "numeric", nullable: false),
                SubApportionmentNumberOfMandates = table.Column<int>(type: "integer", nullable: false),
                RowId = table.Column<Guid>(type: "uuid", nullable: false),
                ColumnId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DoubleProportionalResultCells", x => x.Id);
                table.ForeignKey(
                    name: "FK_DoubleProportionalResultCells_DoubleProportionalResultColum~",
                    column: x => x.ColumnId,
                    principalTable: "DoubleProportionalResultColumns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DoubleProportionalResultCells_DoubleProportionalResultRows_~",
                    column: x => x.RowId,
                    principalTable: "DoubleProportionalResultRows",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DoubleProportionalResultCells_ProportionalElectionLists_Lis~",
                    column: x => x.ListId,
                    principalTable: "ProportionalElectionLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DoubleProportionalResultCells_ColumnId",
            table: "DoubleProportionalResultCells",
            column: "ColumnId");

        migrationBuilder.CreateIndex(
            name: "IX_DoubleProportionalResultCells_ListId",
            table: "DoubleProportionalResultCells",
            column: "ListId");

        migrationBuilder.CreateIndex(
            name: "IX_DoubleProportionalResultCells_RowId",
            table: "DoubleProportionalResultCells",
            column: "RowId");

        migrationBuilder.CreateIndex(
            name: "IX_DoubleProportionalResultColumns_ListId",
            table: "DoubleProportionalResultColumns",
            column: "ListId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DoubleProportionalResultColumns_ResultId",
            table: "DoubleProportionalResultColumns",
            column: "ResultId");

        migrationBuilder.CreateIndex(
            name: "IX_DoubleProportionalResultColumns_UnionListId",
            table: "DoubleProportionalResultColumns",
            column: "UnionListId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DoubleProportionalResultRows_ProportionalElectionId",
            table: "DoubleProportionalResultRows",
            column: "ProportionalElectionId");

        migrationBuilder.CreateIndex(
            name: "IX_DoubleProportionalResultRows_ResultId",
            table: "DoubleProportionalResultRows",
            column: "ResultId");

        migrationBuilder.CreateIndex(
            name: "IX_DoubleProportionalResults_ProportionalElectionId",
            table: "DoubleProportionalResults",
            column: "ProportionalElectionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DoubleProportionalResults_ProportionalElectionUnionId",
            table: "DoubleProportionalResults",
            column: "ProportionalElectionUnionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionUnionEndResults_ProportionalElectionUni~",
            table: "ProportionalElectionUnionEndResults",
            column: "ProportionalElectionUnionId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DoubleProportionalResultCells");

        migrationBuilder.DropTable(
            name: "ProportionalElectionUnionEndResults");

        migrationBuilder.DropTable(
            name: "DoubleProportionalResultColumns");

        migrationBuilder.DropTable(
            name: "DoubleProportionalResultRows");

        migrationBuilder.DropTable(
            name: "DoubleProportionalResults");
    }
}
