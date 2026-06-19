// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.TemporaryData.Migrations;

/// <inheritdoc />
public partial class RenameSecondFactorTransactionNevisData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "ExternalTokenJwtIds",
            table: "SecondFactorTransactions",
            newName: "NevisExternalTokenJwtIds");

        migrationBuilder.RenameColumn(
            name: "ActionId",
            table: "SecondFactorTransactions",
            newName: "ActionIdHash");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "NevisExternalTokenJwtIds",
            table: "SecondFactorTransactions",
            newName: "ExternalTokenJwtIds");

        migrationBuilder.RenameColumn(
            name: "ActionIdHash",
            table: "SecondFactorTransactions",
            newName: "ActionId");
    }
}
