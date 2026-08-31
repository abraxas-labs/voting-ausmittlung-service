// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voting.Ausmittlung.Data.Migrations;

/// <inheritdoc />
public partial class AddECountingCerts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CantonSettingsECountingCertificateChains",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ImportedBy_FirstName = table.Column<string>(type: "text", nullable: false),
                ImportedBy_LastName = table.Column<string>(type: "text", nullable: false),
                ImportedBy_SecureConnectId = table.Column<string>(type: "text", nullable: false),
                RawPem = table.Column<string>(type: "text", nullable: false),
                CantonSettingsId = table.Column<Guid>(type: "uuid", nullable: false),
                ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CantonSettingsECountingCertificateChains", x => x.Id);
                table.ForeignKey(
                    name: "FK_CantonSettingsECountingCertificateChains_CantonSettings_Can~",
                    column: x => x.CantonSettingsId,
                    principalTable: "CantonSettings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CantonSettingsECountingCertificateInfos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CertificateChainId = table.Column<Guid>(type: "uuid", nullable: false),
                RawPem = table.Column<string>(type: "text", nullable: false),
                NotBefore = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                NotAfter = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Issuer = table.Column<string>(type: "text", nullable: false),
                Subject = table.Column<string>(type: "text", nullable: false),
                Thumbprint = table.Column<string>(type: "text", nullable: false),
                CaThumbprint = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CantonSettingsECountingCertificateInfos", x => x.Id);
                table.ForeignKey(
                    name: "FK_CantonSettingsECountingCertificateInfos_CantonSettingsECoun~",
                    column: x => x.CertificateChainId,
                    principalTable: "CantonSettingsECountingCertificateChains",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ResultImportECountingClientCertificateInfos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ResultImportId = table.Column<Guid>(type: "uuid", nullable: false),
                CaCertificateInfoId = table.Column<Guid>(type: "uuid", nullable: false),
                RawPem = table.Column<string>(type: "text", nullable: false),
                NotBefore = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                NotAfter = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Issuer = table.Column<string>(type: "text", nullable: false),
                Subject = table.Column<string>(type: "text", nullable: false),
                Thumbprint = table.Column<string>(type: "text", nullable: false),
                CaThumbprint = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResultImportECountingClientCertificateInfos", x => x.Id);
                table.ForeignKey(
                    name: "FK_ResultImportECountingClientCertificateInfos_CantonSettingsE~",
                    column: x => x.CaCertificateInfoId,
                    principalTable: "CantonSettingsECountingCertificateInfos",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ResultImportECountingClientCertificateInfos_ResultImports_R~",
                    column: x => x.ResultImportId,
                    principalTable: "ResultImports",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CantonSettingsECountingCertificateChains_CantonSettingsId",
            table: "CantonSettingsECountingCertificateChains",
            column: "CantonSettingsId");

        migrationBuilder.CreateIndex(
            name: "IX_CantonSettingsECountingCertificateInfos_CertificateChainId",
            table: "CantonSettingsECountingCertificateInfos",
            column: "CertificateChainId");

        migrationBuilder.CreateIndex(
            name: "IX_ResultImportECountingClientCertificateInfos_CaCertificateIn~",
            table: "ResultImportECountingClientCertificateInfos",
            column: "CaCertificateInfoId");

        migrationBuilder.CreateIndex(
            name: "IX_ResultImportECountingClientCertificateInfos_ResultImportId",
            table: "ResultImportECountingClientCertificateInfos",
            column: "ResultImportId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ResultImportECountingClientCertificateInfos");

        migrationBuilder.DropTable(
            name: "CantonSettingsECountingCertificateInfos");

        migrationBuilder.DropTable(
            name: "CantonSettingsECountingCertificateChains");
    }
}
