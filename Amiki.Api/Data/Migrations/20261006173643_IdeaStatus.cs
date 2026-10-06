using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amiki.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class IdeaStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IdeaId",
                table: "tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ideas",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Parked"); // existing ideas start parked

            migrationBuilder.CreateIndex(
                name: "IX_tasks_IdeaId",
                table: "tasks",
                column: "IdeaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tasks_IdeaId",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "IdeaId",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ideas");
        }
    }
}
