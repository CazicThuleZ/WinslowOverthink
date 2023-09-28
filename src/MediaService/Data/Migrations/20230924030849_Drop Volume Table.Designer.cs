﻿// <auto-generated />
using System;
using MediaService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MediaService.Data.Migrations
{
    [DbContext(typeof(MediaDbContext))]
    [Migration("20230924030849_Drop Volume Table")]
    partial class DropVolumeTable
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("MediaService.Entities.VideoFile", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("DiskVolumeName")
                        .HasColumnType("text");

                    b.Property<decimal>("Duration")
                        .HasColumnType("numeric");

                    b.Property<int>("EpisodeNumber")
                        .HasColumnType("integer");

                    b.Property<string>("EpisodeTitle")
                        .HasColumnType("text");

                    b.Property<DateTime>("FileCreateDateUTC")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("FileName")
                        .HasColumnType("text");

                    b.Property<string>("FilePath")
                        .HasColumnType("text");

                    b.Property<int>("SeasonNumber")
                        .HasColumnType("integer");

                    b.Property<string>("ShowTitle")
                        .HasColumnType("text");

                    b.Property<int>("Size")
                        .HasColumnType("integer");

                    b.Property<string>("ThumbnailUrl")
                        .HasColumnType("text");

                    b.Property<string>("YearReleased")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("VideoFiles");
                });
#pragma warning restore 612, 618
        }
    }
}