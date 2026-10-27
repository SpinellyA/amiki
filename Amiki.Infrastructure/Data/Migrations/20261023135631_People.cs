using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Amiki.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class People : Migration
    {
        // Everything from before Amiki had people is the owner's (AppUser.OwnerId).
        private static readonly Guid Owner = new("00000000-0000-4000-8000-0000000000a1");

        private static readonly string[] PerPersonTables =
        [
            "weekly_notes", "transfers", "transactions", "tasks", "plans", "inbox_items", "ideas", "diary_entries",
            "daily_briefings", "categories", "balance_checks", "audit_log", "assistant_settings", "accounts",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_weekly_notes",
                table: "weekly_notes");

            migrationBuilder.DropIndex(
                name: "IX_weekly_notes_WeekStart",
                table: "weekly_notes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_transfers",
                table: "transfers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_transactions",
                table: "transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tasks",
                table: "tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_plans",
                table: "plans");

            migrationBuilder.DropPrimaryKey(
                name: "PK_inbox_items",
                table: "inbox_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ideas",
                table: "ideas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_diary_entries",
                table: "diary_entries");

            migrationBuilder.DropIndex(
                name: "IX_diary_entries_Day",
                table: "diary_entries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_daily_briefings",
                table: "daily_briefings");

            migrationBuilder.DropIndex(
                name: "IX_daily_briefings_Day",
                table: "daily_briefings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_categories",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_Kind_Name",
                table: "categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_balance_checks",
                table: "balance_checks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_assistant_settings",
                table: "assistant_settings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_accounts",
                table: "accounts");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "weekly_notes",
                type: "uuid",
                nullable: false,
                defaultValue: Owner);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "transfers",
                type: "uuid",
                nullable: false,
                defaultValue: Owner);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "transactions",
                type: "uuid",
                nullable: false,
                defaultValue: Owner);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "tasks",
                type: "uuid",
                nullable: false,
                defaultValue: Owner);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "plans",
                type: "uuid",
                nullable: false,
                defaultValue: Owner);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "inbox_items",
                type: "uuid",
                nullable: false,
                defaultValue: Owner);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ideas",
                type: "uuid",
                nullable: false,
                defaultValue: Owner);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "diary_entries",
                type: "uuid",
                nullable: false,
                defaultValue: Owner);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "daily_briefings",
                type: "uuid",
                nullable: false,
                defaultValue: Owner);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "categories",
                type: "uuid",
                nullable: false,
                defaultValue: Owner);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "balance_checks",
                type: "uuid",
                nullable: false,
                defaultValue: Owner);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "audit_log",
                type: "uuid",
                nullable: false,
                defaultValue: Owner);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "assistant_settings",
                type: "uuid",
                nullable: false,
                defaultValue: Owner);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "accounts",
                type: "uuid",
                nullable: false,
                defaultValue: Owner);

            migrationBuilder.AddPrimaryKey(
                name: "PK_weekly_notes",
                table: "weekly_notes",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_transfers",
                table: "transfers",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_transactions",
                table: "transactions",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_tasks",
                table: "tasks",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_plans",
                table: "plans",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_inbox_items",
                table: "inbox_items",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ideas",
                table: "ideas",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_diary_entries",
                table: "diary_entries",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_daily_briefings",
                table: "daily_briefings",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_categories",
                table: "categories",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_balance_checks",
                table: "balance_checks",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_assistant_settings",
                table: "assistant_settings",
                columns: new[] { "UserId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_accounts",
                table: "accounts",
                columns: new[] { "UserId", "Name" });

            migrationBuilder.CreateTable(
                name: "app_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    GeminiKeyProtected = table.Column<string>(type: "text", nullable: true),
                    GeminiKeySetBy = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    AddedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastSignInAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NoticeSeenAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_weekly_notes_UserId_WeekStart",
                table: "weekly_notes",
                columns: new[] { "UserId", "WeekStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_diary_entries_UserId_Day",
                table: "diary_entries",
                columns: new[] { "UserId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_briefings_UserId_Day",
                table: "daily_briefings",
                columns: new[] { "UserId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_UserId_Kind_Name",
                table: "categories",
                columns: new[] { "UserId", "Kind", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_UserId",
                table: "audit_log",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_app_users_Email",
                table: "app_users",
                column: "Email",
                unique: true);

            // The default only hands existing rows to the owner; new rows always get their person's id from the app.
            foreach (var table in PerPersonTables)
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ALTER COLUMN \"UserId\" DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_weekly_notes",
                table: "weekly_notes");

            migrationBuilder.DropIndex(
                name: "IX_weekly_notes_UserId_WeekStart",
                table: "weekly_notes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_transfers",
                table: "transfers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_transactions",
                table: "transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tasks",
                table: "tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_plans",
                table: "plans");

            migrationBuilder.DropPrimaryKey(
                name: "PK_inbox_items",
                table: "inbox_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ideas",
                table: "ideas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_diary_entries",
                table: "diary_entries");

            migrationBuilder.DropIndex(
                name: "IX_diary_entries_UserId_Day",
                table: "diary_entries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_daily_briefings",
                table: "daily_briefings");

            migrationBuilder.DropIndex(
                name: "IX_daily_briefings_UserId_Day",
                table: "daily_briefings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_categories",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_UserId_Kind_Name",
                table: "categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_balance_checks",
                table: "balance_checks");

            migrationBuilder.DropIndex(
                name: "IX_audit_log_UserId",
                table: "audit_log");

            migrationBuilder.DropPrimaryKey(
                name: "PK_assistant_settings",
                table: "assistant_settings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_accounts",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "weekly_notes");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "transfers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "inbox_items");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ideas");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "diary_entries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "daily_briefings");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "balance_checks");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "audit_log");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "assistant_settings");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "accounts");

            migrationBuilder.AddPrimaryKey(
                name: "PK_weekly_notes",
                table: "weekly_notes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_transfers",
                table: "transfers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_transactions",
                table: "transactions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tasks",
                table: "tasks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_plans",
                table: "plans",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_inbox_items",
                table: "inbox_items",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ideas",
                table: "ideas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_diary_entries",
                table: "diary_entries",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_daily_briefings",
                table: "daily_briefings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_categories",
                table: "categories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_balance_checks",
                table: "balance_checks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_assistant_settings",
                table: "assistant_settings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_accounts",
                table: "accounts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_weekly_notes_WeekStart",
                table: "weekly_notes",
                column: "WeekStart",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_diary_entries_Day",
                table: "diary_entries",
                column: "Day",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_briefings_Day",
                table: "daily_briefings",
                column: "Day",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_Kind_Name",
                table: "categories",
                columns: new[] { "Kind", "Name" },
                unique: true);
        }
    }
}
