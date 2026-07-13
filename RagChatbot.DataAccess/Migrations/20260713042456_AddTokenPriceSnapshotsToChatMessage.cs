using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenPriceSnapshotsToChatMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TokenInCostPerMillion",
                table: "ChatMessages",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TokenOutCostPerMillion",
                table: "ChatMessages",
                type: "numeric",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 13, 4, 24, 55, 952, DateTimeKind.Utc).AddTicks(477) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 13, 4, 24, 55, 952, DateTimeKind.Utc).AddTicks(803) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 13, 4, 24, 55, 952, DateTimeKind.Utc).AddTicks(816) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 13, 4, 24, 55, 952, DateTimeKind.Utc).AddTicks(825) });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 13, 4, 24, 55, 952, DateTimeKind.Utc).AddTicks(379));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenInCostPerMillion",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "TokenOutCostPerMillion",
                table: "ChatMessages");

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 10, 5, 44, 28, 462, DateTimeKind.Utc).AddTicks(2240) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 10, 5, 44, 28, 462, DateTimeKind.Utc).AddTicks(2685) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 10, 5, 44, 28, 462, DateTimeKind.Utc).AddTicks(2714) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 10, 5, 44, 28, 462, DateTimeKind.Utc).AddTicks(2752) });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 10, 5, 44, 28, 462, DateTimeKind.Utc).AddTicks(1932));
        }
    }
}
