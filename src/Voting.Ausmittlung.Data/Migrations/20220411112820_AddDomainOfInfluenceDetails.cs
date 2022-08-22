// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddDomainOfInfluenceDetails : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ContestDomainOfInfluenceDetails",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DomainOfInfluenceId = table.Column<Guid>(type: "uuid", nullable: false),
                ContestId = table.Column<Guid>(type: "uuid", nullable: false),
                TotalCountOfVoters = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContestDomainOfInfluenceDetails", x => x.Id);
                table.ForeignKey(
                    name: "FK_ContestDomainOfInfluenceDetails_Contests_ContestId",
                    column: x => x.ContestId,
                    principalTable: "Contests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ContestDomainOfInfluenceDetails_DomainOfInfluences_DomainOf~",
                    column: x => x.DomainOfInfluenceId,
                    principalTable: "DomainOfInfluences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DomainOfInfluenceCountOfVotersInformationSubTotals",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ContestDomainOfInfluenceDetailsId = table.Column<Guid>(type: "uuid", nullable: false),
                Sex = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters = table.Column<int>(type: "integer", nullable: false),
                VoterType = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainOfInfluenceCountOfVotersInformationSubTotals", x => x.Id);
                table.ForeignKey(
                    name: "FK_DomainOfInfluenceCountOfVotersInformationSubTotals_ContestD~",
                    column: x => x.ContestDomainOfInfluenceDetailsId,
                    principalTable: "ContestDomainOfInfluenceDetails",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DomainOfInfluenceVotingCardResultDetails",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ContestDomainOfInfluenceDetailsId = table.Column<Guid>(type: "uuid", nullable: false),
                CountOfReceivedVotingCards = table.Column<int>(type: "integer", nullable: false),
                Valid = table.Column<bool>(type: "boolean", nullable: false),
                Channel = table.Column<int>(type: "integer", nullable: false),
                DomainOfInfluenceType = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DomainOfInfluenceVotingCardResultDetails", x => x.Id);
                table.ForeignKey(
                    name: "FK_DomainOfInfluenceVotingCardResultDetails_ContestDomainOfInf~",
                    column: x => x.ContestDomainOfInfluenceDetailsId,
                    principalTable: "ContestDomainOfInfluenceDetails",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ContestDomainOfInfluenceDetails_ContestId",
            table: "ContestDomainOfInfluenceDetails",
            column: "ContestId");

        migrationBuilder.CreateIndex(
            name: "IX_ContestDomainOfInfluenceDetails_DomainOfInfluenceId",
            table: "ContestDomainOfInfluenceDetails",
            column: "DomainOfInfluenceId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCountOfVotersInformationSubTotals_ContestD~",
            table: "DomainOfInfluenceCountOfVotersInformationSubTotals",
            columns: new[] { "ContestDomainOfInfluenceDetailsId", "Sex", "VoterType" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceVotingCardResultDetails_ContestDomainOfInf~",
            table: "DomainOfInfluenceVotingCardResultDetails",
            columns: new[] { "ContestDomainOfInfluenceDetailsId", "Channel", "Valid", "DomainOfInfluenceType" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DomainOfInfluenceCountOfVotersInformationSubTotals");

        migrationBuilder.DropTable(
            name: "DomainOfInfluenceVotingCardResultDetails");

        migrationBuilder.DropTable(
            name: "ContestDomainOfInfluenceDetails");
    }
}
