// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

public partial class MigrateProportionalElectionMandateAlgorithms : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CantonDefaults_ProportionalElectionMandateAlgorithms",
            table: "DomainOfInfluences");

        migrationBuilder.DropColumn(
            name: "ProportionalElectionMandateAlgorithms",
            table: "CantonSettings");

        migrationBuilder.Sql(@"
                UPDATE ""ProportionalElections""
                SET ""MandateAlgorithm"" = 4
                WHERE ""MandateAlgorithm"" = 2;

                UPDATE ""ProportionalElections""
                SET ""MandateAlgorithm"" = 6
                WHERE ""MandateAlgorithm"" = 3;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<List<int>>(
            name: "CantonDefaults_ProportionalElectionMandateAlgorithms",
            table: "DomainOfInfluences",
            type: "integer[]",
            nullable: false);

        migrationBuilder.AddColumn<List<int>>(
            name: "ProportionalElectionMandateAlgorithms",
            table: "CantonSettings",
            type: "integer[]",
            nullable: false);
    }
}
