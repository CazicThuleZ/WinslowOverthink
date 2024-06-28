﻿// <auto-generated />
using System;
using DashboardService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DashboardService.Data.Migrations
{
    [DbContext(typeof(DashboardDbContext))]
    [Migration("20240628191050_AddThreeTables")]
    partial class AddThreeTables
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("DashboardService.Entities.AccountBalance", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("AccountName")
                        .HasColumnType("text");

                    b.Property<decimal>("Balance")
                        .HasColumnType("numeric");

                    b.Property<DateTime>("SnapshotDateUTC")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("AccountBalances");
                });

            modelBuilder.Entity("DashboardService.Entities.ActivityCount", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("Count")
                        .HasColumnType("numeric");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<DateTime>("SnapshotDateUTC")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("ActivityCount");
                });

            modelBuilder.Entity("DashboardService.Entities.ActivityDuration", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<TimeSpan>("Duration")
                        .HasColumnType("interval");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<DateTime>("SnapshotDateUTC")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("ActivityDuration");
                });

            modelBuilder.Entity("DashboardService.Entities.CryptoPrice", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("CryptoId")
                        .HasColumnType("text");

                    b.Property<decimal>("Price")
                        .HasColumnType("numeric");

                    b.Property<DateTime>("SnapshotDateUTC")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("CryptoPrices");
                });

            modelBuilder.Entity("DashboardService.Entities.DietStat", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("Calories")
                        .HasColumnType("integer");

                    b.Property<decimal>("CarbGrams")
                        .HasColumnType("numeric");

                    b.Property<decimal>("CholesterolGrams")
                        .HasColumnType("numeric");

                    b.Property<decimal>("Cost")
                        .HasColumnType("numeric");

                    b.Property<int>("ExerciseCalories")
                        .HasColumnType("integer");

                    b.Property<decimal>("FatGrams")
                        .HasColumnType("numeric");

                    b.Property<decimal>("FiberGrams")
                        .HasColumnType("numeric");

                    b.Property<decimal>("ProteinGrams")
                        .HasColumnType("numeric");

                    b.Property<decimal>("SaturatedFatGrams")
                        .HasColumnType("numeric");

                    b.Property<DateTime>("SnapshotDateUTC")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("SodiumMilliGrams")
                        .HasColumnType("numeric");

                    b.Property<decimal>("SugarGrams")
                        .HasColumnType("numeric");

                    b.Property<decimal>("Weight")
                        .HasColumnType("numeric");

                    b.HasKey("Id");

                    b.ToTable("DietStats");
                });

            modelBuilder.Entity("DashboardService.Entities.FoodPrice", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("LastUpdateDateUTC")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<decimal>("Price")
                        .HasColumnType("numeric");

                    b.Property<string>("UnitOfMeasure")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("FoodPrices");
                });
#pragma warning restore 612, 618
        }
    }
}
