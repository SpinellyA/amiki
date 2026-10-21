using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amiki.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssistantCanPropose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanPropose",
                table: "assistant_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true); // on, like for new settings
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanPropose",
                table: "assistant_settings");
        }
    }
}
