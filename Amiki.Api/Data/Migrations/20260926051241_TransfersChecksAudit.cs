using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Amiki.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TransfersChecksAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Name",
                keyValue: "BPI");

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Entity = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Before = table.Column<string>(type: "jsonb", nullable: true),
                    After = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "balance_checks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Account = table.Column<string>(type: "text", nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Expected = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Actual = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    AdjustmentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_balance_checks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "transfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    From = table.Column<string>(type: "text", nullable: false),
                    To = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Fee = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfers", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Name",
                keyValue: "Cash",
                column: "OpeningBalance",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Name",
                keyValue: "GCash",
                column: "OpeningBalance",
                value: 0m);

            migrationBuilder.InsertData(
                table: "accounts",
                columns: new[] { "Name", "OpeningBalance" },
                values: new object[] { "Landbank", 0m });

            // BPI became Landbank: entries already on BPI move with it.
            migrationBuilder.Sql("""UPDATE transactions SET "Account" = 'Landbank' WHERE "Account" = 'BPI';""");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_At",
                table: "audit_log",
                column: "At");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_EntityId",
                table: "audit_log",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_balance_checks_Account_CheckedAt",
                table: "balance_checks",
                columns: new[] { "Account", "CheckedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_transfers_Date",
                table: "transfers",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "balance_checks");

            migrationBuilder.DropTable(
                name: "transfers");

            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "Name",
                keyValue: "Landbank");

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Name",
                keyValue: "Cash",
                column: "OpeningBalance",
                value: 800m);

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "Name",
                keyValue: "GCash",
                column: "OpeningBalance",
                value: 1200m);

            migrationBuilder.InsertData(
                table: "accounts",
                columns: new[] { "Name", "OpeningBalance" },
                values: new object[] { "BPI", 15000m });

            migrationBuilder.Sql("""UPDATE transactions SET "Account" = 'BPI' WHERE "Account" = 'Landbank';""");
        }
    }
}
