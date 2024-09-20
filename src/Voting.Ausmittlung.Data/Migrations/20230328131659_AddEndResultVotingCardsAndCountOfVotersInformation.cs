// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class AddEndResultVotingCardsAndCountOfVotersInformation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MajorityElectionEndResultCountOfVotersInformationSubTotal",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MajorityElectionEndResultId = table.Column<Guid>(type: "uuid", nullable: false),
                Sex = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters = table.Column<int>(type: "integer", nullable: true),
                VoterType = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionEndResultCountOfVotersInformationSubTotal", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionEndResultCountOfVotersInformationSubTotal_M~",
                    column: x => x.MajorityElectionEndResultId,
                    principalTable: "MajorityElectionEndResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MajorityElectionEndResultVotingCardDetail",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MajorityElectionEndResultId = table.Column<Guid>(type: "uuid", nullable: false),
                CountOfReceivedVotingCards = table.Column<int>(type: "integer", nullable: true),
                Valid = table.Column<bool>(type: "boolean", nullable: false),
                Channel = table.Column<int>(type: "integer", nullable: false),
                DomainOfInfluenceType = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MajorityElectionEndResultVotingCardDetail", x => x.Id);
                table.ForeignKey(
                    name: "FK_MajorityElectionEndResultVotingCardDetail_MajorityElectionE~",
                    column: x => x.MajorityElectionEndResultId,
                    principalTable: "MajorityElectionEndResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionEndResultCountOfVotersInformationSubTotal",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionEndResultId = table.Column<Guid>(type: "uuid", nullable: false),
                Sex = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters = table.Column<int>(type: "integer", nullable: true),
                VoterType = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionEndResultCountOfVotersInformationSubTot~", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionEndResultCountOfVotersInformationSubTot~",
                    column: x => x.ProportionalElectionEndResultId,
                    principalTable: "ProportionalElectionEndResult",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProportionalElectionEndResultVotingCardDetail",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProportionalElectionEndResultId = table.Column<Guid>(type: "uuid", nullable: false),
                CountOfReceivedVotingCards = table.Column<int>(type: "integer", nullable: true),
                Valid = table.Column<bool>(type: "boolean", nullable: false),
                Channel = table.Column<int>(type: "integer", nullable: false),
                DomainOfInfluenceType = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProportionalElectionEndResultVotingCardDetail", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProportionalElectionEndResultVotingCardDetail_ProportionalE~",
                    column: x => x.ProportionalElectionEndResultId,
                    principalTable: "ProportionalElectionEndResult",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "VoteEndResultCountOfVotersInformationSubTotal",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                VoteEndResultId = table.Column<Guid>(type: "uuid", nullable: false),
                Sex = table.Column<int>(type: "integer", nullable: false),
                CountOfVoters = table.Column<int>(type: "integer", nullable: true),
                VoterType = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VoteEndResultCountOfVotersInformationSubTotal", x => x.Id);
                table.ForeignKey(
                    name: "FK_VoteEndResultCountOfVotersInformationSubTotal_VoteEndResult~",
                    column: x => x.VoteEndResultId,
                    principalTable: "VoteEndResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "VoteEndResultVotingCardDetail",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                VoteEndResultId = table.Column<Guid>(type: "uuid", nullable: false),
                CountOfReceivedVotingCards = table.Column<int>(type: "integer", nullable: true),
                Valid = table.Column<bool>(type: "boolean", nullable: false),
                Channel = table.Column<int>(type: "integer", nullable: false),
                DomainOfInfluenceType = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VoteEndResultVotingCardDetail", x => x.Id);
                table.ForeignKey(
                    name: "FK_VoteEndResultVotingCardDetail_VoteEndResults_VoteEndResultId",
                    column: x => x.VoteEndResultId,
                    principalTable: "VoteEndResults",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionEndResultCountOfVotersInformationSubTotal_M~",
            table: "MajorityElectionEndResultCountOfVotersInformationSubTotal",
            column: "MajorityElectionEndResultId");

        migrationBuilder.CreateIndex(
            name: "IX_MajorityElectionEndResultVotingCardDetail_MajorityElectionE~",
            table: "MajorityElectionEndResultVotingCardDetail",
            column: "MajorityElectionEndResultId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionEndResultCountOfVotersInformationSubTot~",
            table: "ProportionalElectionEndResultCountOfVotersInformationSubTotal",
            column: "ProportionalElectionEndResultId");

        migrationBuilder.CreateIndex(
            name: "IX_ProportionalElectionEndResultVotingCardDetail_ProportionalE~",
            table: "ProportionalElectionEndResultVotingCardDetail",
            column: "ProportionalElectionEndResultId");

        migrationBuilder.CreateIndex(
            name: "IX_VoteEndResultCountOfVotersInformationSubTotal_VoteEndResult~",
            table: "VoteEndResultCountOfVotersInformationSubTotal",
            column: "VoteEndResultId");

        migrationBuilder.CreateIndex(
            name: "IX_VoteEndResultVotingCardDetail_VoteEndResultId",
            table: "VoteEndResultVotingCardDetail",
            column: "VoteEndResultId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MajorityElectionEndResultCountOfVotersInformationSubTotal");

        migrationBuilder.DropTable(
            name: "MajorityElectionEndResultVotingCardDetail");

        migrationBuilder.DropTable(
            name: "ProportionalElectionEndResultCountOfVotersInformationSubTotal");

        migrationBuilder.DropTable(
            name: "ProportionalElectionEndResultVotingCardDetail");

        migrationBuilder.DropTable(
            name: "VoteEndResultCountOfVotersInformationSubTotal");

        migrationBuilder.DropTable(
            name: "VoteEndResultVotingCardDetail");
    }
}
