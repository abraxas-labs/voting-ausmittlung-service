// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddDoubleProportionalResultLotDecisions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "AllNumberOfMandatesDistributed",
            table: "DoubleProportionalResults",
            newName: "HasSuperApportionmentRequiredLotDecision");

        migrationBuilder.RenameColumn(
            name: "SuperApportionmentNumberOfMandatesUnrounded",
            table: "DoubleProportionalResultColumns",
            newName: "SuperApportionmentQuotient");

        migrationBuilder.RenameColumn(
            name: "SuperApportionmentNumberOfMandates",
            table: "DoubleProportionalResultColumns",
            newName: "SuperApportionmentNumberOfMandatesFromLotDecision");

        migrationBuilder.RenameColumn(
            name: "SubApportionmentNumberOfMandates",
            table: "DoubleProportionalResultCells",
            newName: "SubApportionmentNumberOfMandatesFromLotDecision");

        migrationBuilder.AlterColumn<decimal>(
            name: "ElectionKey",
            table: "DoubleProportionalResults",
            type: "numeric",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AddColumn<bool>(
            name: "HasSubApportionmentRequiredLotDecision",
            table: "DoubleProportionalResults",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "SubApportionmentState",
            table: "DoubleProportionalResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "SuperApportionmentNumberOfMandatesForLotDecision",
            table: "DoubleProportionalResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "SuperApportionmentState",
            table: "DoubleProportionalResults",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "SubApportionmentInitialNegativeTies",
            table: "DoubleProportionalResultColumns",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "SuperApportionmentLotDecisionRequired",
            table: "DoubleProportionalResultColumns",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "SuperApportionmentNumberOfMandatesExclLotDecision",
            table: "DoubleProportionalResultColumns",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<bool>(
            name: "SubApportionmentLotDecisionRequired",
            table: "DoubleProportionalResultCells",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "SubApportionmentNumberOfMandatesExclLotDecision",
            table: "DoubleProportionalResultCells",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HasSubApportionmentRequiredLotDecision",
            table: "DoubleProportionalResults");

        migrationBuilder.DropColumn(
            name: "SubApportionmentState",
            table: "DoubleProportionalResults");

        migrationBuilder.DropColumn(
            name: "SuperApportionmentNumberOfMandatesForLotDecision",
            table: "DoubleProportionalResults");

        migrationBuilder.DropColumn(
            name: "SuperApportionmentState",
            table: "DoubleProportionalResults");

        migrationBuilder.DropColumn(
            name: "SubApportionmentInitialNegativeTies",
            table: "DoubleProportionalResultColumns");

        migrationBuilder.DropColumn(
            name: "SuperApportionmentLotDecisionRequired",
            table: "DoubleProportionalResultColumns");

        migrationBuilder.DropColumn(
            name: "SuperApportionmentNumberOfMandatesExclLotDecision",
            table: "DoubleProportionalResultColumns");

        migrationBuilder.DropColumn(
            name: "SubApportionmentLotDecisionRequired",
            table: "DoubleProportionalResultCells");

        migrationBuilder.DropColumn(
            name: "SubApportionmentNumberOfMandatesExclLotDecision",
            table: "DoubleProportionalResultCells");

        migrationBuilder.RenameColumn(
            name: "HasSuperApportionmentRequiredLotDecision",
            table: "DoubleProportionalResults",
            newName: "AllNumberOfMandatesDistributed");

        migrationBuilder.RenameColumn(
            name: "SuperApportionmentQuotient",
            table: "DoubleProportionalResultColumns",
            newName: "SuperApportionmentNumberOfMandatesUnrounded");

        migrationBuilder.RenameColumn(
            name: "SuperApportionmentNumberOfMandatesFromLotDecision",
            table: "DoubleProportionalResultColumns",
            newName: "SuperApportionmentNumberOfMandates");

        migrationBuilder.RenameColumn(
            name: "SubApportionmentNumberOfMandatesFromLotDecision",
            table: "DoubleProportionalResultCells",
            newName: "SubApportionmentNumberOfMandates");

        migrationBuilder.AlterColumn<int>(
            name: "ElectionKey",
            table: "DoubleProportionalResults",
            type: "integer",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric");
    }
}
