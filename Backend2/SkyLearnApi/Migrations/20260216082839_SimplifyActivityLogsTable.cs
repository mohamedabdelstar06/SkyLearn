using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyLearnApi.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyActivityLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActivityLog_Jti",
                table: "ActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLog_SessionId",
                table: "ActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLog_UserId_LoginTime",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "Jti",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "LoginTime",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "LogoutTime",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "SessionDurationSeconds",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "ActivityLogs");

            migrationBuilder.AddColumn<string>(
                name: "UserFullName",
                table: "ActivityLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // Backfill UserFullName for all existing records from AspNetUsers
            migrationBuilder.Sql(@"
                UPDATE AL
                SET AL.UserFullName = U.FullName
                FROM ActivityLogs AL
                INNER JOIN AspNetUsers U ON AL.UserId = U.Id
                WHERE AL.UserFullName = '' OR AL.UserFullName IS NULL;

                UPDATE ActivityLogs
                SET UserFullName = 'Guest user'
                WHERE UserFullName = '' OR UserFullName IS NULL;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLog_UserFullName",
                table: "ActivityLogs",
                column: "UserFullName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActivityLog_UserFullName",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "UserFullName",
                table: "ActivityLogs");

            migrationBuilder.AddColumn<string>(
                name: "Jti",
                table: "ActivityLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LoginTime",
                table: "ActivityLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LogoutTime",
                table: "ActivityLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SessionDurationSeconds",
                table: "ActivityLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "ActivityLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLog_Jti",
                table: "ActivityLogs",
                column: "Jti",
                filter: "[Jti] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLog_SessionId",
                table: "ActivityLogs",
                column: "SessionId",
                filter: "[SessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLog_UserId_LoginTime",
                table: "ActivityLogs",
                columns: new[] { "UserId", "LoginTime" },
                filter: "[LoginTime] IS NOT NULL");
        }
    }
}
