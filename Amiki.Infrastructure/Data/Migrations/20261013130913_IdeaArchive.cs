using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amiki.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class IdeaArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "ideas",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DoneAt",
                table: "ideas",
                type: "timestamp without time zone",
                nullable: true);

            // Ideas already done have no finish date; start their archive countdown now.
            migrationBuilder.Sql("""UPDATE ideas SET "DoneAt" = now()::timestamp WHERE "Status" = 'Done';""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "ideas");

            migrationBuilder.DropColumn(
                name: "DoneAt",
                table: "ideas");
        }
    }
}
