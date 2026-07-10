using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenBillingAndTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TokenIn",
                table: "ChatMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenOut",
                table: "ChatMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UsdRate",
                table: "ChatMessages",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsdVndRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastQueryDate",
                value: new DateTime(2026, 7, 10, 5, 44, 28, 462, DateTimeKind.Utc).AddTicks(2240));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastQueryDate",
                value: new DateTime(2026, 7, 10, 5, 44, 28, 462, DateTimeKind.Utc).AddTicks(2685));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastQueryDate",
                value: new DateTime(2026, 7, 10, 5, 44, 28, 462, DateTimeKind.Utc).AddTicks(2714));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                column: "LastQueryDate",
                value: new DateTime(2026, 7, 10, 5, 44, 28, 462, DateTimeKind.Utc).AddTicks(2752));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 10, 5, 44, 28, 462, DateTimeKind.Utc).AddTicks(1932));

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropColumn(
                name: "TokenIn",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "TokenOut",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "UsdRate",
                table: "ChatMessages");

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastQueryDate",
                value: new DateTime(2026, 7, 10, 3, 59, 53, 404, DateTimeKind.Utc).AddTicks(6208));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastQueryDate",
                value: new DateTime(2026, 7, 10, 3, 59, 53, 404, DateTimeKind.Utc).AddTicks(6704));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastQueryDate",
                value: new DateTime(2026, 7, 10, 3, 59, 53, 404, DateTimeKind.Utc).AddTicks(6720));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                column: "LastQueryDate",
                value: new DateTime(2026, 7, 10, 3, 59, 53, 404, DateTimeKind.Utc).AddTicks(6732));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 10, 3, 59, 53, 404, DateTimeKind.Utc).AddTicks(6069));
        }
    }
}
