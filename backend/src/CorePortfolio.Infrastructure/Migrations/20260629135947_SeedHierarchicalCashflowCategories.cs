using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CorePortfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedHierarchicalCashflowCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentCategoryId",
                table: "CashflowCategories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "CashflowCategories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000001"),
                columns: new[] { "Icon", "ParentCategoryId", "SortOrder" },
                values: new object[] { "💵", null, 1 });

            migrationBuilder.UpdateData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000002"),
                columns: new[] { "ParentCategoryId", "SortOrder" },
                values: new object[] { new Guid("00000000-0000-0000-0001-000000000001"), 2 });

            migrationBuilder.UpdateData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000003"),
                columns: new[] { "Icon", "Name", "ParentCategoryId", "SortOrder" },
                values: new object[] { "💼", "Thu nhập phụ", null, 2 });

            migrationBuilder.UpdateData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000001"),
                columns: new[] { "Icon", "ParentCategoryId", "SortOrder" },
                values: new object[] { "🍜", null, 1 });

            migrationBuilder.UpdateData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000002"),
                columns: new[] { "Color", "Icon", "ParentCategoryId", "SortOrder" },
                values: new object[] { "#F97316", "🏘️", new Guid("00000000-0000-0000-0001-a00000000002"), 1 });

            migrationBuilder.UpdateData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000003"),
                columns: new[] { "Color", "Name", "ParentCategoryId", "SortOrder" },
                values: new object[] { "#EAB308", "Di chuyển", null, 3 });

            migrationBuilder.UpdateData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000004"),
                columns: new[] { "Color", "Icon", "ParentCategoryId", "SortOrder" },
                values: new object[] { "#8B5CF6", "🎭", null, 6 });

            migrationBuilder.UpdateData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000005"),
                columns: new[] { "ParentCategoryId", "SortOrder" },
                values: new object[] { new Guid("00000000-0000-0000-0001-a00000000003"), 5 });

            migrationBuilder.InsertData(
                table: "CashflowCategories",
                columns: new[] { "Id", "Color", "Icon", "IsGlobal", "Name", "ParentCategoryId", "SortOrder", "Type", "UserId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0001-a00000000001"), "#A7F3D0", "🎁", true, "Khác", null, 3, 1, null },
                    { new Guid("00000000-0000-0000-0001-a00000000002"), "#F97316", "🏠", true, "Chỗ ở & Cố định", null, 2, 2, null },
                    { new Guid("00000000-0000-0000-0001-a00000000003"), "#3B82F6", "🧴", true, "Sinh hoạt & Cá nhân", null, 4, 2, null },
                    { new Guid("00000000-0000-0000-0001-a00000000004"), "#EC4899", "🎁", true, "Quan hệ xã hội", null, 5, 2, null },
                    { new Guid("00000000-0000-0000-0001-a00000000005"), "#06B6D4", "✈️", true, "Du lịch", null, 7, 2, null },
                    { new Guid("00000000-0000-0000-0001-a00000000006"), "#14B8A6", "📚", true, "Học tập", null, 8, 2, null },
                    { new Guid("00000000-0000-0000-0001-a00000000007"), "#64748B", "❓", true, "Khác", null, 9, 2, null },
                    { new Guid("00000000-0000-0000-0003-000000000001"), "#8B5CF6", "📈", true, "Đầu tư", null, 1, 3, null },
                    { new Guid("00000000-0000-0000-0004-000000000001"), "#0EA5E9", "💰", true, "Tiết kiệm", null, 1, 4, null },
                    { new Guid("1e3efabd-a3bd-4584-82fd-122d282fa63d"), "#FACC15", "🚕", true, "Grab/Taxi", new Guid("00000000-0000-0000-0002-000000000003"), 2, 2, null },
                    { new Guid("3e25eb4b-adb7-4fbc-b8ad-bf13093056d3"), "#047857", "📦", true, "Bán hàng online", new Guid("00000000-0000-0000-0001-000000000003"), 2, 1, null },
                    { new Guid("3ec84eb1-f674-4db2-acae-5b95b5552c63"), "#8B5CF6", "🎬", true, "Xem phim", new Guid("00000000-0000-0000-0002-000000000004"), 1, 2, null },
                    { new Guid("61930dcc-89b9-43db-a3fb-8d17279be178"), "#FEF08A", "🔧", true, "Bảo dưỡng xe", new Guid("00000000-0000-0000-0002-000000000003"), 3, 2, null },
                    { new Guid("694f5dee-6171-4b45-be98-b12e46510a73"), "#10B981", "💵", true, "Lương chính", new Guid("00000000-0000-0000-0001-000000000001"), 1, 1, null },
                    { new Guid("6f8052ba-84d6-49db-b84a-2f8ef4643540"), "#EF4444", "🍚", true, "Hằng ngày", new Guid("00000000-0000-0000-0002-000000000001"), 1, 2, null },
                    { new Guid("8ab71e3f-24bf-498f-b04b-4b053c2a0b3a"), "#EAB308", "⛽", true, "Xăng xe", new Guid("00000000-0000-0000-0002-000000000003"), 1, 2, null },
                    { new Guid("900175b4-e02d-4504-a20b-863206362b60"), "#059669", "💻", true, "Freelance", new Guid("00000000-0000-0000-0001-000000000003"), 1, 1, null },
                    { new Guid("b46a571f-7c9d-4b73-b3a3-da32fb178a5e"), "#A78BFA", "🎮", true, "Game/Sub", new Guid("00000000-0000-0000-0002-000000000004"), 2, 2, null },
                    { new Guid("c34be7d4-b7cf-40b5-9120-adb21e7bb52a"), "#F87171", "🍽️", true, "Ăn ngoài", new Guid("00000000-0000-0000-0002-000000000001"), 2, 2, null },
                    { new Guid("dd537949-bade-4d30-ac2a-62b6710b0fa7"), "#6EE7B7", "🕒", true, "OT", new Guid("00000000-0000-0000-0001-000000000001"), 3, 1, null },
                    { new Guid("df4c8022-9f49-4beb-94aa-397650e13b57"), "#FCA5A5", "☕", true, "Coffee/Trà sữa", new Guid("00000000-0000-0000-0002-000000000001"), 3, 2, null },
                    { new Guid("00fc27d6-be2a-4cfb-99aa-5ae00b0d4617"), "#38BDF8", "🏦", true, "Gửi ngân hàng", new Guid("00000000-0000-0000-0004-000000000001"), 2, 4, null },
                    { new Guid("3b4c2644-2aac-4eee-b147-a24f15369ab2"), "#3B82F6", "🛒", true, "Đồ dùng sinh hoạt", new Guid("00000000-0000-0000-0001-a00000000003"), 1, 2, null },
                    { new Guid("4b31b1d3-e5e3-4423-8ce2-03a0b2c707e0"), "#BFDBFE", "💊", true, "Y tế", new Guid("00000000-0000-0000-0001-a00000000003"), 4, 2, null },
                    { new Guid("5fc975e0-42f1-4fe4-bd58-45d6853a54e5"), "#FDBA74", "💧", true, "Nước", new Guid("00000000-0000-0000-0001-a00000000002"), 3, 2, null },
                    { new Guid("66292db5-3c78-4892-ae54-b93e40d14a7b"), "#FFEDD5", "🌐", true, "Internet", new Guid("00000000-0000-0000-0001-a00000000002"), 4, 2, null },
                    { new Guid("773de4db-23b8-41b3-b752-140e3fc71a23"), "#8B5CF6", "₿", true, "Crypto", new Guid("00000000-0000-0000-0003-000000000001"), 1, 3, null },
                    { new Guid("a53443b6-aeb0-4b3b-8e41-215ec23acd4a"), "#60A5FA", "👕", true, "Quần áo", new Guid("00000000-0000-0000-0001-a00000000003"), 2, 2, null },
                    { new Guid("af1f5f06-79b7-4888-80cd-86d4695a8072"), "#F472B6", "🎁", true, "Quà tặng", new Guid("00000000-0000-0000-0001-a00000000004"), 2, 2, null },
                    { new Guid("b6820223-9133-4b7c-b77e-bc17b40df075"), "#A78BFA", "📊", true, "Cổ phiếu", new Guid("00000000-0000-0000-0003-000000000001"), 2, 3, null },
                    { new Guid("c50f14de-c0cc-4de1-926a-40252384f0b0"), "#EC4899", "💌", true, "Hiếu hỉ", new Guid("00000000-0000-0000-0001-a00000000004"), 1, 2, null },
                    { new Guid("ccdaf802-c248-439f-9d8a-7c22ee25e3d8"), "#C4B5FD", "🏦", true, "Chứng chỉ quỹ", new Guid("00000000-0000-0000-0003-000000000001"), 3, 3, null },
                    { new Guid("e45cb584-d6bf-44e0-beeb-5bc1cb3960cb"), "#FB923C", "⚡", true, "Điện", new Guid("00000000-0000-0000-0001-a00000000002"), 2, 2, null },
                    { new Guid("fb25f644-e903-47de-a5a6-e4f2018e4a4f"), "#93C5FD", "✂️", true, "Cắt tóc", new Guid("00000000-0000-0000-0001-a00000000003"), 3, 2, null },
                    { new Guid("fcae34ef-35d9-4cfa-beb1-2cc6838f6bd9"), "#0EA5E9", "🛡️", true, "Quỹ khẩn cấp", new Guid("00000000-0000-0000-0004-000000000001"), 1, 4, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashflowCategories_ParentCategoryId",
                table: "CashflowCategories",
                column: "ParentCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashflowCategories_CashflowCategories_ParentCategoryId",
                table: "CashflowCategories",
                column: "ParentCategoryId",
                principalTable: "CashflowCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashflowCategories_CashflowCategories_ParentCategoryId",
                table: "CashflowCategories");

            migrationBuilder.DropIndex(
                name: "IX_CashflowCategories_ParentCategoryId",
                table: "CashflowCategories");

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-a00000000001"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-a00000000005"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-a00000000006"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-a00000000007"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00fc27d6-be2a-4cfb-99aa-5ae00b0d4617"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("1e3efabd-a3bd-4584-82fd-122d282fa63d"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("3b4c2644-2aac-4eee-b147-a24f15369ab2"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("3e25eb4b-adb7-4fbc-b8ad-bf13093056d3"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("3ec84eb1-f674-4db2-acae-5b95b5552c63"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("4b31b1d3-e5e3-4423-8ce2-03a0b2c707e0"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("5fc975e0-42f1-4fe4-bd58-45d6853a54e5"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("61930dcc-89b9-43db-a3fb-8d17279be178"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("66292db5-3c78-4892-ae54-b93e40d14a7b"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("694f5dee-6171-4b45-be98-b12e46510a73"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("6f8052ba-84d6-49db-b84a-2f8ef4643540"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("773de4db-23b8-41b3-b752-140e3fc71a23"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("8ab71e3f-24bf-498f-b04b-4b053c2a0b3a"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("900175b4-e02d-4504-a20b-863206362b60"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("a53443b6-aeb0-4b3b-8e41-215ec23acd4a"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("af1f5f06-79b7-4888-80cd-86d4695a8072"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("b46a571f-7c9d-4b73-b3a3-da32fb178a5e"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("b6820223-9133-4b7c-b77e-bc17b40df075"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("c34be7d4-b7cf-40b5-9120-adb21e7bb52a"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("c50f14de-c0cc-4de1-926a-40252384f0b0"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("ccdaf802-c248-439f-9d8a-7c22ee25e3d8"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("dd537949-bade-4d30-ac2a-62b6710b0fa7"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("df4c8022-9f49-4beb-94aa-397650e13b57"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("e45cb584-d6bf-44e0-beeb-5bc1cb3960cb"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("fb25f644-e903-47de-a5a6-e4f2018e4a4f"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("fcae34ef-35d9-4cfa-beb1-2cc6838f6bd9"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-a00000000002"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-a00000000003"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-a00000000004"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0003-000000000001"));

            migrationBuilder.DeleteData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0004-000000000001"));

            migrationBuilder.DropColumn(
                name: "ParentCategoryId",
                table: "CashflowCategories");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "CashflowCategories");

            migrationBuilder.UpdateData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000001"),
                column: "Icon",
                value: "💰");

            migrationBuilder.UpdateData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000003"),
                columns: new[] { "Icon", "Name" },
                values: new object[] { "📈", "Đầu tư" });

            migrationBuilder.UpdateData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000001"),
                column: "Icon",
                value: "🍔");

            migrationBuilder.UpdateData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000002"),
                columns: new[] { "Color", "Icon" },
                values: new object[] { "#F87171", "🏠" });

            migrationBuilder.UpdateData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000003"),
                columns: new[] { "Color", "Name" },
                values: new object[] { "#FCA5A5", "Đi lại" });

            migrationBuilder.UpdateData(
                table: "CashflowCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000004"),
                columns: new[] { "Color", "Icon" },
                values: new object[] { "#B91C1C", "🎮" });
        }
    }
}
