// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.TemporaryData.Migrations;

/// <inheritdoc />
public partial class RemoveSecondFactorExternalIdentifier : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ExternalIdentifier",
            table: "SecondFactorTransactions");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ExternalIdentifier",
            table: "SecondFactorTransactions",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);
    }
}
