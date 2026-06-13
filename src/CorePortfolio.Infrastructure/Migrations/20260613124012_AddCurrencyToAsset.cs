using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorePortfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyToAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Assets",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Assets");
        }
    }
}
