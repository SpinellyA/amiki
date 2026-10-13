using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amiki.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class WeeklyNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "weekly_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekStart = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    WentWell = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ToChange = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_notes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_weekly_notes_WeekStart",
                table: "weekly_notes",
                column: "WeekStart",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weekly_notes");
        }
    }
}
