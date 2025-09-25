// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddDoiTypeToSubTotal : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_DomainOfInfluenceCountOfVotersInformationSubTotals_ContestD~",
            table: "DomainOfInfluenceCountOfVotersInformationSubTotals");

        migrationBuilder.DropIndex(
            name: "IX_CountOfVotersInformationSubTotals_ContestCountingCircleDeta~",
            table: "CountOfVotersInformationSubTotals");

        migrationBuilder.DropIndex(
            name: "IX_ContestCountOfVotersInformationSubTotals_ContestDetailsId_S~",
            table: "ContestCountOfVotersInformationSubTotals");

        migrationBuilder.DropColumn(
            name: "TotalCountOfVoters",
            table: "ContestDomainOfInfluenceDetails");

        migrationBuilder.DropColumn(
            name: "TotalCountOfVoters",
            table: "ContestDetails");

        migrationBuilder.DropColumn(
            name: "TotalCountOfVoters",
            table: "ContestCountingCircleDetails");

        migrationBuilder.AddColumn<int>(
            name: "DomainOfInfluenceType",
            table: "VoteEndResultCountOfVotersInformationSubTotal",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "DomainOfInfluenceType",
            table: "ProportionalElectionEndResultCountOfVotersInformationSubTotal",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "DomainOfInfluenceType",
            table: "MajorityElectionEndResultCountOfVotersInformationSubTotal",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "DomainOfInfluenceType",
            table: "DomainOfInfluenceCountOfVotersInformationSubTotals",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "DomainOfInfluenceType",
            table: "CountOfVotersInformationSubTotals",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "DomainOfInfluenceType",
            table: "ContestCountOfVotersInformationSubTotals",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCountOfVotersInformationSubTotals_ContestD~",
            table: "DomainOfInfluenceCountOfVotersInformationSubTotals",
            columns: new[] { "ContestDomainOfInfluenceDetailsId", "Sex", "VoterType", "DomainOfInfluenceType" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CountOfVotersInformationSubTotals_ContestCountingCircleDeta~",
            table: "CountOfVotersInformationSubTotals",
            columns: new[] { "ContestCountingCircleDetailsId", "Sex", "VoterType", "DomainOfInfluenceType" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ContestCountOfVotersInformationSubTotals_ContestDetailsId_S~",
            table: "ContestCountOfVotersInformationSubTotals",
            columns: new[] { "ContestDetailsId", "Sex", "VoterType", "DomainOfInfluenceType" },
            unique: true);

        migrationBuilder.Sql(@"
INSERT INTO ""CountOfVotersInformationSubTotals"" (""Id"", ""ContestCountingCircleDetailsId"", ""VoterType"", ""Sex"", ""CountOfVoters"", ""DomainOfInfluenceType"")
SELECT DISTINCT
    gen_random_uuid(),
    st.""ContestCountingCircleDetailsId"", 
    st.""VoterType"", 
    st.""Sex"",
    st.""CountOfVoters"",
    vc.""DomainOfInfluenceType""
FROM ""CountOfVotersInformationSubTotals"" st
JOIN ""VotingCardResultDetails"" vc 
    ON st.""ContestCountingCircleDetailsId"" = vc.""ContestCountingCircleDetailsId""
ON CONFLICT (""ContestCountingCircleDetailsId"", ""VoterType"", ""Sex"", ""DomainOfInfluenceType"") DO NOTHING;

DELETE FROM ""CountOfVotersInformationSubTotals""
WHERE ""DomainOfInfluenceType""=0;
            ");

        migrationBuilder.Sql(@"
INSERT INTO ""DomainOfInfluenceCountOfVotersInformationSubTotals"" (""Id"", ""ContestDomainOfInfluenceDetailsId"", ""VoterType"", ""Sex"", ""CountOfVoters"", ""DomainOfInfluenceType"")
SELECT
    gen_random_uuid(),
    st.""ContestDomainOfInfluenceDetailsId"", 
    st.""VoterType"", 
    st.""Sex"",
    st.""CountOfVoters"",
    vc.""DomainOfInfluenceType""
FROM ""DomainOfInfluenceCountOfVotersInformationSubTotals"" st
JOIN ""DomainOfInfluenceVotingCardResultDetails"" vc 
    ON st.""ContestDomainOfInfluenceDetailsId"" = vc.""ContestDomainOfInfluenceDetailsId""
ON CONFLICT (""ContestDomainOfInfluenceDetailsId"", ""VoterType"", ""Sex"", ""DomainOfInfluenceType"") DO NOTHING;

DELETE FROM ""DomainOfInfluenceCountOfVotersInformationSubTotals""
WHERE ""DomainOfInfluenceType""=0;
            ");

        migrationBuilder.Sql(@"
INSERT INTO ""ContestCountOfVotersInformationSubTotals"" (""Id"", ""ContestDetailsId"", ""VoterType"", ""Sex"", ""CountOfVoters"", ""DomainOfInfluenceType"")
SELECT
    gen_random_uuid(),
    st.""ContestDetailsId"", 
    st.""VoterType"", 
    st.""Sex"",
    st.""CountOfVoters"",
    vc.""DomainOfInfluenceType""
FROM ""ContestCountOfVotersInformationSubTotals"" st
JOIN ""ContestVotingCardResultDetails"" vc 
    ON st.""ContestDetailsId"" = vc.""ContestDetailsId""
ON CONFLICT (""ContestDetailsId"", ""VoterType"", ""Sex"", ""DomainOfInfluenceType"") DO NOTHING;

DELETE FROM ""ContestCountOfVotersInformationSubTotals""
WHERE ""DomainOfInfluenceType""=0;
            ");

        migrationBuilder.Sql(@"
UPDATE ""VoteEndResultCountOfVotersInformationSubTotal""
SET ""DomainOfInfluenceType""=x.""DomainOfInfluenceType""
FROM 
(
	SELECT doi.""Type"" AS ""DomainOfInfluenceType""
	FROM ""VoteEndResultCountOfVotersInformationSubTotal"" st
	JOIN ""VoteEndResults"" endr ON st.""VoteEndResultId""= endr.""Id""
	JOIN ""Votes"" pb ON pb.""Id"" = endr.""VoteId""
	JOIN ""DomainOfInfluences"" doi ON doi.""Id"" = pb.""DomainOfInfluenceId""
) x
            ");

        migrationBuilder.Sql(@"
UPDATE ""ProportionalElectionEndResultCountOfVotersInformationSubTotal""
SET ""DomainOfInfluenceType""=x.""DomainOfInfluenceType""
FROM 
(
	SELECT doi.""Type"" AS ""DomainOfInfluenceType""
	FROM ""ProportionalElectionEndResultCountOfVotersInformationSubTotal"" st
	JOIN ""ProportionalElectionEndResult"" endr ON st.""ProportionalElectionEndResultId""= endr.""Id""
	JOIN ""ProportionalElections"" pb ON pb.""Id"" = endr.""ProportionalElectionId""
	JOIN ""DomainOfInfluences"" doi ON doi.""Id"" = pb.""DomainOfInfluenceId""
) x
            ");

        migrationBuilder.Sql(@"
UPDATE ""MajorityElectionEndResultCountOfVotersInformationSubTotal""
SET ""DomainOfInfluenceType""=x.""DomainOfInfluenceType""
FROM 
(
	SELECT doi.""Type"" AS ""DomainOfInfluenceType""
	FROM ""MajorityElectionEndResultCountOfVotersInformationSubTotal"" st
	JOIN ""MajorityElectionEndResults"" endr ON st.""MajorityElectionEndResultId""= endr.""Id""
	JOIN ""MajorityElections"" pb ON pb.""Id"" = endr.""MajorityElectionId""
	JOIN ""DomainOfInfluences"" doi ON doi.""Id"" = pb.""DomainOfInfluenceId""
) x
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_DomainOfInfluenceCountOfVotersInformationSubTotals_ContestD~",
            table: "DomainOfInfluenceCountOfVotersInformationSubTotals");

        migrationBuilder.DropIndex(
            name: "IX_CountOfVotersInformationSubTotals_ContestCountingCircleDeta~",
            table: "CountOfVotersInformationSubTotals");

        migrationBuilder.DropIndex(
            name: "IX_ContestCountOfVotersInformationSubTotals_ContestDetailsId_S~",
            table: "ContestCountOfVotersInformationSubTotals");

        migrationBuilder.DropColumn(
            name: "DomainOfInfluenceType",
            table: "VoteEndResultCountOfVotersInformationSubTotal");

        migrationBuilder.DropColumn(
            name: "DomainOfInfluenceType",
            table: "ProportionalElectionEndResultCountOfVotersInformationSubTotal");

        migrationBuilder.DropColumn(
            name: "DomainOfInfluenceType",
            table: "MajorityElectionEndResultCountOfVotersInformationSubTotal");

        migrationBuilder.DropColumn(
            name: "DomainOfInfluenceType",
            table: "DomainOfInfluenceCountOfVotersInformationSubTotals");

        migrationBuilder.DropColumn(
            name: "DomainOfInfluenceType",
            table: "CountOfVotersInformationSubTotals");

        migrationBuilder.DropColumn(
            name: "DomainOfInfluenceType",
            table: "ContestCountOfVotersInformationSubTotals");

        migrationBuilder.AddColumn<int>(
            name: "TotalCountOfVoters",
            table: "ContestDomainOfInfluenceDetails",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "TotalCountOfVoters",
            table: "ContestDetails",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "TotalCountOfVoters",
            table: "ContestCountingCircleDetails",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCountOfVotersInformationSubTotals_ContestD~",
            table: "DomainOfInfluenceCountOfVotersInformationSubTotals",
            columns: new[] { "ContestDomainOfInfluenceDetailsId", "Sex", "VoterType" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CountOfVotersInformationSubTotals_ContestCountingCircleDeta~",
            table: "CountOfVotersInformationSubTotals",
            columns: new[] { "ContestCountingCircleDetailsId", "Sex", "VoterType" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ContestCountOfVotersInformationSubTotals_ContestDetailsId_S~",
            table: "ContestCountOfVotersInformationSubTotals",
            columns: new[] { "ContestDetailsId", "Sex", "VoterType" },
            unique: true);
    }
}
