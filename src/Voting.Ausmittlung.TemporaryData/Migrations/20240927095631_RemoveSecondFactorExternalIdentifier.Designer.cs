﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Voting.Ausmittlung.TemporaryData;

#nullable disable

namespace Voting.Ausmittlung.TemporaryData.Migrations
{
    [DbContext(typeof(TemporaryDataContext))]
    [Migration("20240927095631_RemoveSecondFactorExternalIdentifier")]
    partial class RemoveSecondFactorExternalIdentifier
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Voting.Ausmittlung.TemporaryData.Models.ExportLogEntry", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ExportKey")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("TenantId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("ExportLogEntries");
                });

            modelBuilder.Entity("Voting.Ausmittlung.TemporaryData.Models.SecondFactorTransaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ActionId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("ExpiredAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<List<string>>("ExternalTokenJwtIds")
                        .HasColumnType("text[]");

                    b.Property<DateTime>("LastUpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("PollCount")
                        .HasColumnType("integer");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("SecondFactorTransactions");
                });
#pragma warning restore 612, 618
        }
    }
}
