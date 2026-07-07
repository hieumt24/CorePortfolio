using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorePortfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModel_AzureFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "MarketAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastPriceError",
                table: "MarketAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceSource",
                table: "MarketAssets",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PriceStatus",
                table: "MarketAssets",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "ExternalId", "LastPriceError", "PriceSource", "PriceStatus" },
                values: new object[] { null, null, "Manual", "Manual" });

            migrationBuilder.UpdateData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                columns: new[] { "ExternalId", "LastPriceError", "PriceSource", "PriceStatus" },
                values: new object[] { null, null, "Manual", "Manual" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "MarketAssets");

            migrationBuilder.DropColumn(
                name: "LastPriceError",
                table: "MarketAssets");

            migrationBuilder.DropColumn(
                name: "PriceSource",
                table: "MarketAssets");

            migrationBuilder.DropColumn(
                name: "PriceStatus",
                table: "MarketAssets");
        }
    }
}
