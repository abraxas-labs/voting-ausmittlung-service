// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddDomainOfInfluenceSuperiorAuthority : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "SuperiorAuthorityDomainOfInfluenceId",
            table: "DomainOfInfluences",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_DomainOfInfluences_SuperiorAuthorityDomainOfInfluenceId",
            table: "DomainOfInfluences",
            column: "SuperiorAuthorityDomainOfInfluenceId");

        migrationBuilder.AddForeignKey(
            name: "FK_DomainOfInfluences_DomainOfInfluences_SuperiorAuthorityDoma~",
            table: "DomainOfInfluences",
            column: "SuperiorAuthorityDomainOfInfluenceId",
            principalTable: "DomainOfInfluences",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_DomainOfInfluences_DomainOfInfluences_SuperiorAuthorityDoma~",
            table: "DomainOfInfluences");

        migrationBuilder.DropIndex(
            name: "IX_DomainOfInfluences_SuperiorAuthorityDomainOfInfluenceId",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "SuperiorAuthorityDomainOfInfluenceId",
            table: "DomainOfInfluences");
    }
}
