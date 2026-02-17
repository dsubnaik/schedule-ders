using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace schedule_ders.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseCrnField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CourseCrn",
                table: "Courses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseCrn",
                table: "Courses");
        }
    }
}
