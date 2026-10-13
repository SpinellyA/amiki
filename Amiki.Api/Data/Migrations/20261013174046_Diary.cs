using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amiki.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Diary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "diary_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Mood = table.Column<int>(type: "integer", nullable: true),
                    Text = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diary_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_diary_entries_Day",
                table: "diary_entries",
                column: "Day",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "diary_entries");
        }
    }
}
