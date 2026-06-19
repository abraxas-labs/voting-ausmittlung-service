// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddSelectDivisorCalculations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ElectionKeyCalculation_LowerBoundary_Dividend",
            table: "DoubleProportionalResults",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "ElectionKeyCalculation_LowerBoundary_Divisor",
            table: "DoubleProportionalResults",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<decimal>(
            name: "ElectionKeyCalculation_LowerBoundary_Quotient",
            table: "DoubleProportionalResults",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "ElectionKeyCalculation_SelectDivisor_Dividend",
            table: "DoubleProportionalResults",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "ElectionKeyCalculation_SelectDivisor_Divisor",
            table: "DoubleProportionalResults",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<decimal>(
            name: "ElectionKeyCalculation_SelectDivisor_Quotient",
            table: "DoubleProportionalResults",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "ElectionKeyCalculation_UpperBoundary_Dividend",
            table: "DoubleProportionalResults",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "ElectionKeyCalculation_UpperBoundary_Divisor",
            table: "DoubleProportionalResults",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<decimal>(
            name: "ElectionKeyCalculation_UpperBoundary_Quotient",
            table: "DoubleProportionalResults",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "DivisorCalculation_LowerBoundary_Dividend",
            table: "DoubleProportionalResultRows",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "DivisorCalculation_LowerBoundary_Divisor",
            table: "DoubleProportionalResultRows",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<decimal>(
            name: "DivisorCalculation_LowerBoundary_Quotient",
            table: "DoubleProportionalResultRows",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "DivisorCalculation_SelectDivisor_Dividend",
            table: "DoubleProportionalResultRows",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "DivisorCalculation_SelectDivisor_Divisor",
            table: "DoubleProportionalResultRows",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<decimal>(
            name: "DivisorCalculation_SelectDivisor_Quotient",
            table: "DoubleProportionalResultRows",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "DivisorCalculation_UpperBoundary_Dividend",
            table: "DoubleProportionalResultRows",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "DivisorCalculation_UpperBoundary_Divisor",
            table: "DoubleProportionalResultRows",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<decimal>(
            name: "DivisorCalculation_UpperBoundary_Quotient",
            table: "DoubleProportionalResultRows",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "DivisorCalculation_LowerBoundary_Dividend",
            table: "DoubleProportionalResultColumns",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "DivisorCalculation_LowerBoundary_Divisor",
            table: "DoubleProportionalResultColumns",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<decimal>(
            name: "DivisorCalculation_LowerBoundary_Quotient",
            table: "DoubleProportionalResultColumns",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "DivisorCalculation_SelectDivisor_Dividend",
            table: "DoubleProportionalResultColumns",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "DivisorCalculation_SelectDivisor_Divisor",
            table: "DoubleProportionalResultColumns",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<decimal>(
            name: "DivisorCalculation_SelectDivisor_Quotient",
            table: "DoubleProportionalResultColumns",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "DivisorCalculation_UpperBoundary_Dividend",
            table: "DoubleProportionalResultColumns",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "DivisorCalculation_UpperBoundary_Divisor",
            table: "DoubleProportionalResultColumns",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<decimal>(
            name: "DivisorCalculation_UpperBoundary_Quotient",
            table: "DoubleProportionalResultColumns",
            type: "numeric",
            nullable: false,
            defaultValue: 0m);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ElectionKeyCalculation_LowerBoundary_Dividend",
            table: "DoubleProportionalResults");

        migrationBuilder.DropColumn(
            name: "ElectionKeyCalculation_LowerBoundary_Divisor",
            table: "DoubleProportionalResults");

        migrationBuilder.DropColumn(
            name: "ElectionKeyCalculation_LowerBoundary_Quotient",
            table: "DoubleProportionalResults");

        migrationBuilder.DropColumn(
            name: "ElectionKeyCalculation_SelectDivisor_Dividend",
            table: "DoubleProportionalResults");

        migrationBuilder.DropColumn(
            name: "ElectionKeyCalculation_SelectDivisor_Divisor",
            table: "DoubleProportionalResults");

        migrationBuilder.DropColumn(
            name: "ElectionKeyCalculation_SelectDivisor_Quotient",
            table: "DoubleProportionalResults");

        migrationBuilder.DropColumn(
            name: "ElectionKeyCalculation_UpperBoundary_Dividend",
            table: "DoubleProportionalResults");

        migrationBuilder.DropColumn(
            name: "ElectionKeyCalculation_UpperBoundary_Divisor",
            table: "DoubleProportionalResults");

        migrationBuilder.DropColumn(
            name: "ElectionKeyCalculation_UpperBoundary_Quotient",
            table: "DoubleProportionalResults");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_LowerBoundary_Dividend",
            table: "DoubleProportionalResultRows");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_LowerBoundary_Divisor",
            table: "DoubleProportionalResultRows");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_LowerBoundary_Quotient",
            table: "DoubleProportionalResultRows");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_SelectDivisor_Dividend",
            table: "DoubleProportionalResultRows");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_SelectDivisor_Divisor",
            table: "DoubleProportionalResultRows");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_SelectDivisor_Quotient",
            table: "DoubleProportionalResultRows");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_UpperBoundary_Dividend",
            table: "DoubleProportionalResultRows");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_UpperBoundary_Divisor",
            table: "DoubleProportionalResultRows");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_UpperBoundary_Quotient",
            table: "DoubleProportionalResultRows");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_LowerBoundary_Dividend",
            table: "DoubleProportionalResultColumns");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_LowerBoundary_Divisor",
            table: "DoubleProportionalResultColumns");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_LowerBoundary_Quotient",
            table: "DoubleProportionalResultColumns");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_SelectDivisor_Dividend",
            table: "DoubleProportionalResultColumns");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_SelectDivisor_Divisor",
            table: "DoubleProportionalResultColumns");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_SelectDivisor_Quotient",
            table: "DoubleProportionalResultColumns");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_UpperBoundary_Dividend",
            table: "DoubleProportionalResultColumns");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_UpperBoundary_Divisor",
            table: "DoubleProportionalResultColumns");

        migrationBuilder.DropColumn(
            name: "DivisorCalculation_UpperBoundary_Quotient",
            table: "DoubleProportionalResultColumns");
    }
}
