using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CorePortfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceBenchmarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenchmarkDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    MarketAssetId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AssetGroup = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenchmarkDefinitions_MarketAssets_MarketAssetId",
                        column: x => x.MarketAssetId,
                        principalTable: "MarketAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkPricePoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BenchmarkDefinitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosePrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    QualityStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkPricePoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenchmarkPricePoints_BenchmarkDefinitions_BenchmarkDefinitionId",
                        column: x => x.BenchmarkDefinitionId,
                        principalTable: "BenchmarkDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "BenchmarkDefinitions",
                columns: new[] { "Id", "AssetGroup", "CreatedAt", "Currency", "IsActive", "IsDefault", "MarketAssetId", "Name", "Symbol" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0005-000000000001"), "Stock", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "VND", true, true, null, "VN-Index", "VNINDEX" },
                    { new Guid("00000000-0000-0000-0005-000000000002"), "Crypto", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD", true, true, null, "Bitcoin", "BTC" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkDefinitions_AssetGroup_IsActive_IsDefault",
                table: "BenchmarkDefinitions",
                columns: new[] { "AssetGroup", "IsActive", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkDefinitions_MarketAssetId",
                table: "BenchmarkDefinitions",
                column: "MarketAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkPricePoints_BenchmarkDefinitionId_Date",
                table: "BenchmarkPricePoints",
                columns: new[] { "BenchmarkDefinitionId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenchmarkPricePoints");

            migrationBuilder.DropTable(
                name: "BenchmarkDefinitions");
        }
    }
}
