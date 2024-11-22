// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddSourceDomainOfInfluenceId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_DomainOfInfluenceCountingCircles_CountingCircleId_DomainOfI~",
            table: "DomainOfInfluenceCountingCircles");

        migrationBuilder.DropColumn(
            name: "Inherited",
            table: "DomainOfInfluenceCountingCircles");

        migrationBuilder.AddColumn<Guid>(
            name: "SourceDomainOfInfluenceId",
            table: "DomainOfInfluenceCountingCircles",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCountingCircles_CountingCircleId_DomainOfI~",
            table: "DomainOfInfluenceCountingCircles",
            columns: new[] { "CountingCircleId", "DomainOfInfluenceId", "SourceDomainOfInfluenceId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_DomainOfInfluenceCountingCircles_CountingCircleId_DomainOfI~",
            table: "DomainOfInfluenceCountingCircles");

        migrationBuilder.DropColumn(
            name: "SourceDomainOfInfluenceId",
            table: "DomainOfInfluenceCountingCircles");

        migrationBuilder.AddColumn<bool>(
            name: "Inherited",
            table: "DomainOfInfluenceCountingCircles",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluenceCountingCircles_CountingCircleId_DomainOfI~",
            table: "DomainOfInfluenceCountingCircles",
            columns: new[] { "CountingCircleId", "DomainOfInfluenceId" },
            unique: true);
    }
}
