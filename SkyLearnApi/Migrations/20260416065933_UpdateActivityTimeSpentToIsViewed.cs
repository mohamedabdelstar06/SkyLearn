using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyLearnApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateActivityTimeSpentToIsViewed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalTimeSpentSeconds",
                table: "StudentActivityProgress");

            migrationBuilder.AddColumn<bool>(
                name: "IsViewed",
                table: "StudentActivityProgress",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsViewed",
                table: "StudentActivityProgress");

            migrationBuilder.AddColumn<long>(
                name: "TotalTimeSpentSeconds",
                table: "StudentActivityProgress",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
