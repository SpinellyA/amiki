using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amiki.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MayaToMaribank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Name",
                keyValue: "Maya");

            migrationBuilder.InsertData(
                table: "accounts",
                columns: new[] { "Name", "OpeningBalance" },
                values: new object[] { "Maribank", 0m });

            // Maya became Maribank: everything recorded on Maya moves with it, so the balance carries over.
            migrationBuilder.Sql("""
                UPDATE transactions SET "Account" = 'Maribank' WHERE "Account" = 'Maya';
                UPDATE transfers SET "From" = 'Maribank' WHERE "From" = 'Maya';
                UPDATE transfers SET "To" = 'Maribank' WHERE "To" = 'Maya';
                UPDATE balance_checks SET "Account" = 'Maribank' WHERE "Account" = 'Maya';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Name",
                keyValue: "Maribank");

            migrationBuilder.InsertData(
                table: "accounts",
                columns: new[] { "Name", "OpeningBalance" },
                values: new object[] { "Maya", 0m });

            migrationBuilder.Sql("""
                UPDATE transactions SET "Account" = 'Maya' WHERE "Account" = 'Maribank';
                UPDATE transfers SET "From" = 'Maya' WHERE "From" = 'Maribank';
                UPDATE transfers SET "To" = 'Maya' WHERE "To" = 'Maribank';
                UPDATE balance_checks SET "Account" = 'Maya' WHERE "Account" = 'Maribank';
                """);
        }
    }
}
