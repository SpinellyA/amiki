using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Amiki.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Categories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Icon = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "Id", "CreatedAt", "Icon", "Kind", "Name" },
                values: new object[,]
                {
                    { new Guid("1f0c8a01-0000-4000-8000-000000000001"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "Restaurant", "Expense", "Food" },
                    { new Guid("1f0c8a01-0000-4000-8000-000000000002"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "DirectionsCar", "Expense", "Transport" },
                    { new Guid("1f0c8a01-0000-4000-8000-000000000003"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "School", "Expense", "School" },
                    { new Guid("1f0c8a01-0000-4000-8000-000000000004"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "PhoneAndroid", "Expense", "Load & data" },
                    { new Guid("1f0c8a01-0000-4000-8000-000000000005"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "ReceiptLong", "Expense", "Bills" },
                    { new Guid("1f0c8a01-0000-4000-8000-000000000006"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "ShoppingBag", "Expense", "Shopping" },
                    { new Guid("1f0c8a01-0000-4000-8000-000000000007"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "LocalHospital", "Expense", "Health" },
                    { new Guid("1f0c8a01-0000-4000-8000-000000000008"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "SportsEsports", "Expense", "Fun" },
                    { new Guid("1f0c8a01-0000-4000-8000-000000000009"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "Toll", "Expense", "Fees" },
                    { new Guid("1f0c8a01-0000-4000-8000-00000000000a"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "MoreHoriz", "Expense", "Other" },
                    { new Guid("1f0c8a01-0000-4000-8000-00000000000b"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "FactCheck", "Expense", "Balance fix" },
                    { new Guid("1f0c8a01-0000-4000-8000-000000000101"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "Savings", "Income", "Allowance" },
                    { new Guid("1f0c8a01-0000-4000-8000-000000000102"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "Work", "Income", "Salary" },
                    { new Guid("1f0c8a01-0000-4000-8000-000000000103"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "CardGiftcard", "Income", "Gift" },
                    { new Guid("1f0c8a01-0000-4000-8000-000000000104"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "Replay", "Income", "Refund" },
                    { new Guid("1f0c8a01-0000-4000-8000-000000000105"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "AttachMoney", "Income", "Other income" },
                    { new Guid("1f0c8a01-0000-4000-8000-000000000106"), new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "FactCheck", "Income", "Balance fix" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_categories_Kind_Name",
                table: "categories",
                columns: new[] { "Kind", "Name" },
                unique: true);

            // Any category name already on a transaction but not in the starter list gets its own
            // category, so existing transactions stay valid and editable.
            migrationBuilder.Sql("""
                INSERT INTO categories ("Id", "Name", "Kind", "Icon", "CreatedAt")
                SELECT gen_random_uuid(), t."Category", t."Kind", 'Label', now()::timestamp
                FROM (SELECT DISTINCT "Category", "Kind" FROM transactions) t
                WHERE NOT EXISTS (SELECT 1 FROM categories c WHERE c."Name" = t."Category" AND c."Kind" = t."Kind");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
