using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amiki.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class WishlistSaving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IncomeShare",
                table: "wishlist_funds",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceHistory",
                table: "wish_items",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "Rank",
                table: "wish_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Saved",
                table: "wish_items",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "TargetDate",
                table: "wish_items",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncomeShare",
                table: "wishlist_funds");

            migrationBuilder.DropColumn(
                name: "PriceHistory",
                table: "wish_items");

            migrationBuilder.DropColumn(
                name: "Rank",
                table: "wish_items");

            migrationBuilder.DropColumn(
                name: "Saved",
                table: "wish_items");

            migrationBuilder.DropColumn(
                name: "TargetDate",
                table: "wish_items");
        }
    }
}
