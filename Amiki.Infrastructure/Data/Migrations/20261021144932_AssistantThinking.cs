using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amiki.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssistantThinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Thinking",
                table: "assistant_settings",
                type: "text",
                nullable: false,
                defaultValue: "low"); // existing settings start on Quick
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Thinking",
                table: "assistant_settings");
        }
    }
}
