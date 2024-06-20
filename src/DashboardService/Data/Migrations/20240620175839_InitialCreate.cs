using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DashboardService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDateUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AccountName = table.Column<string>(type: "text", nullable: true),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CryptoPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDateUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CryptoId = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CryptoPrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DietStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDateUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Calories = table.Column<int>(type: "integer", nullable: false),
                    ExerciseCalories = table.Column<int>(type: "integer", nullable: false),
                    CarbGrams = table.Column<decimal>(type: "numeric", nullable: false),
                    ProteinGrams = table.Column<decimal>(type: "numeric", nullable: false),
                    FatGrams = table.Column<decimal>(type: "numeric", nullable: false),
                    SaturatedFatGrams = table.Column<decimal>(type: "numeric", nullable: false),
                    SugarGrams = table.Column<decimal>(type: "numeric", nullable: false),
                    CholesterolGrams = table.Column<decimal>(type: "numeric", nullable: false),
                    FiberGrams = table.Column<decimal>(type: "numeric", nullable: false),
                    SodiumMilliGrams = table.Column<decimal>(type: "numeric", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietStats", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountBalances");

            migrationBuilder.DropTable(
                name: "CryptoPrices");

            migrationBuilder.DropTable(
                name: "DietStats");
        }
    }
}
