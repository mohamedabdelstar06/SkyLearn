using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyLearnApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAssignmentFileConstraints_AddAssignmentFileUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedExtensions",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "MaxFileSizeMB",
                table: "Activities");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentFileUrls",
                table: "Activities",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignmentFileUrls",
                table: "Activities");

            migrationBuilder.AddColumn<string>(
                name: "AllowedExtensions",
                table: "Activities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxFileSizeMB",
                table: "Activities",
                type: "int",
                nullable: true);
        }
    }
}
