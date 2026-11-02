using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amiki.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Budgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "budgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Length = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Start = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Repeats = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budgets", x => new { x.UserId, x.Id });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budgets");
        }
    }
}
