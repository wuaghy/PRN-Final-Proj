using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectLecturer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LecturerId",
                table: "Subjects",
                type: "integer",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_LecturerId",
                table: "Subjects",
                column: "LecturerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_AppUsers_LecturerId",
                table: "Subjects",
                column: "LecturerId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_AppUsers_LecturerId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_LecturerId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "LecturerId",
                table: "Subjects");

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 23, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 23, 6, 38, 38, 179, DateTimeKind.Utc).AddTicks(3930) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 23, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 23, 6, 38, 38, 179, DateTimeKind.Utc).AddTicks(4198) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 23, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 23, 6, 38, 38, 179, DateTimeKind.Utc).AddTicks(4213) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 23, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 23, 6, 38, 38, 179, DateTimeKind.Utc).AddTicks(4224) });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 23, 6, 38, 38, 179, DateTimeKind.Utc).AddTicks(3817));
        }
    }
}
