using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 10, 3, 59, 53, 404, DateTimeKind.Utc).AddTicks(6208) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 10, 3, 59, 53, 404, DateTimeKind.Utc).AddTicks(6704) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 10, 3, 59, 53, 404, DateTimeKind.Utc).AddTicks(6720) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 10, 3, 59, 53, 404, DateTimeKind.Utc).AddTicks(6732) });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 10, 3, 59, 53, 404, DateTimeKind.Utc).AddTicks(6069));

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_Key",
                table: "AppSettings",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 9, 1, 22, 5, 20, DateTimeKind.Utc).AddTicks(8215) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 9, 1, 22, 5, 20, DateTimeKind.Utc).AddTicks(8531) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 9, 1, 22, 5, 20, DateTimeKind.Utc).AddTicks(8545) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 9, 1, 22, 5, 20, DateTimeKind.Utc).AddTicks(8564) });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 1, 22, 5, 20, DateTimeKind.Utc).AddTicks(8100));
        }
    }
}
