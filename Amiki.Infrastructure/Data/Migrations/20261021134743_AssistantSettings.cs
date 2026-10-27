using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amiki.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssistantSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assistant_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AboutMe = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Personality = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SeeTasks = table.Column<bool>(type: "boolean", nullable: false),
                    SeeIdeas = table.Column<bool>(type: "boolean", nullable: false),
                    SeeMoney = table.Column<bool>(type: "boolean", nullable: false),
                    SeeInbox = table.Column<bool>(type: "boolean", nullable: false),
                    SeeWeeklyNotes = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistant_settings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assistant_settings");
        }
    }
}
