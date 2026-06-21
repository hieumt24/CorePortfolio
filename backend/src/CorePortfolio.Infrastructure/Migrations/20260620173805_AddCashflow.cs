using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CorePortfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCashflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashflowCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    IsGlobal = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashflowCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashflowCategories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashflowRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PortfolioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashflowRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashflowRecords_CashflowCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CashflowCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashflowRecords_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashflowRecords_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CashflowRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AssetCategories",
                columns: new[] { "Id", "DefaultCurrency", "Name" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), "VND", "Fiat" });

            migrationBuilder.InsertData(
                table: "CashflowCategories",
                columns: new[] { "Id", "Color", "Icon", "IsGlobal", "Name", "Type", "UserId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0001-000000000001"), "#10B981", "💰", true, "Lương", 1, null },
                    { new Guid("00000000-0000-0000-0001-000000000002"), "#34D399", "🎁", true, "Thưởng", 1, null },
                    { new Guid("00000000-0000-0000-0001-000000000003"), "#059669", "📈", true, "Đầu tư", 1, null },
                    { new Guid("00000000-0000-0000-0002-000000000001"), "#EF4444", "🍔", true, "Ăn uống", 2, null },
                    { new Guid("00000000-0000-0000-0002-000000000002"), "#F87171", "🏠", true, "Tiền nhà", 2, null },
                    { new Guid("00000000-0000-0000-0002-000000000003"), "#FCA5A5", "🚗", true, "Đi lại", 2, null },
                    { new Guid("00000000-0000-0000-0002-000000000004"), "#B91C1C", "🎮", true, "Giải trí", 2, null },
                    { new Guid("00000000-0000-0000-0002-000000000005"), "#DC2626", "🛍️", true, "Mua sắm", 2, null }
                });

            migrationBuilder.InsertData(
                table: "MarketAssets",
                columns: new[] { "Id", "CategoryId", "CurrentPrice", "LastUpdated", "Name", "Symbol" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000002"), new Guid("00000000-0000-0000-0000-000000000001"), 1m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "VND Cash", "VND" },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000001"), 1m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD Cash", "USD" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashflowCategories_UserId",
                table: "CashflowCategories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CashflowRecords_CategoryId",
                table: "CashflowRecords",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CashflowRecords_PortfolioId",
                table: "CashflowRecords",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_CashflowRecords_TransactionId",
                table: "CashflowRecords",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashflowRecords_UserId",
                table: "CashflowRecords",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashflowRecords");

            migrationBuilder.DropTable(
                name: "CashflowCategories");

            migrationBuilder.DeleteData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "MarketAssets",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "AssetCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));
        }
    }
}
