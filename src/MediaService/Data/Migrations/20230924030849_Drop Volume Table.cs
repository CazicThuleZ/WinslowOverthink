using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaService.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropVolumeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiskVolumes");

            migrationBuilder.DropColumn(
                name: "DiskVolumeId",
                table: "VideoFiles");

            migrationBuilder.AddColumn<string>(
                name: "DiskVolumeName",
                table: "VideoFiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YearReleased",
                table: "VideoFiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiskVolumeName",
                table: "VideoFiles");

            migrationBuilder.DropColumn(
                name: "YearReleased",
                table: "VideoFiles");

            migrationBuilder.AddColumn<Guid>(
                name: "DiskVolumeId",
                table: "VideoFiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "DiskVolumes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailableSpace = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiskVolumes", x => x.Id);
                });
        }
    }
}
