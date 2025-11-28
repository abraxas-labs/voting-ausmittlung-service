// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddMajorityElectionCandidatePartyLongDesc : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Party",
            table: "SecondaryMajorityElectionCandidateTranslations",
            newName: "PartyShortDescription");

        migrationBuilder.RenameColumn(
            name: "Party",
            table: "MajorityElectionCandidateTranslations",
            newName: "PartyShortDescription");

        migrationBuilder.AddColumn<string>(
            name: "PartyLongDescription",
            table: "SecondaryMajorityElectionCandidateTranslations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "PartyLongDescription",
            table: "MajorityElectionCandidateTranslations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PartyLongDescription",
            table: "SecondaryMajorityElectionCandidateTranslations");

        migrationBuilder.DropColumn(
            name: "PartyLongDescription",
            table: "MajorityElectionCandidateTranslations");

        migrationBuilder.RenameColumn(
            name: "PartyShortDescription",
            table: "SecondaryMajorityElectionCandidateTranslations",
            newName: "Party");

        migrationBuilder.RenameColumn(
            name: "PartyShortDescription",
            table: "MajorityElectionCandidateTranslations",
            newName: "Party");
    }
}
