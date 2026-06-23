using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkyLearnApi.Migrations
{
    /// <inheritdoc />
    public partial class BackfillUserFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Backfill UserFullName from AspNetUsers for records that have a UserId
            migrationBuilder.Sql(@"
                UPDATE AL
                SET AL.UserFullName = U.FullName
                FROM ActivityLogs AL
                INNER JOIN AspNetUsers U ON AL.UserId = U.Id
                WHERE AL.UserFullName = '' OR AL.UserFullName IS NULL;
            ");

            // Step 2: Set 'Guest user' for any remaining records without a user match
            migrationBuilder.Sql(@"
                UPDATE ActivityLogs
                SET UserFullName = 'Guest user'
                WHERE UserFullName = '' OR UserFullName IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed - data backfill is a one-way operation
        }
    }
}
