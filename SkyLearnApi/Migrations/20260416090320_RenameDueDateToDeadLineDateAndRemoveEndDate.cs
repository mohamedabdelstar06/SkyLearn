using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyLearnApi.Migrations
{
    /// <inheritdoc />
    public partial class RenameDueDateToDeadLineDateAndRemoveEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Activities");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "Activities",
                newName: "DeadLineDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DeadLineDate",
                table: "Activities",
                newName: "EndDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Activities",
                type: "datetime2",
                nullable: true);
        }
    }
}
