using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace schedule_ders.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseTitlesAndRequestedCourseTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestedCourseTitle",
                table: "SIRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CourseTitle",
                table: "Courses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CourseTitle",
                table: "CourseCatalogEntries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE [Courses]
                SET [CourseTitle] = [CourseName]
                WHERE [CourseTitle] = ''
                """);

            migrationBuilder.Sql(
                """
                UPDATE [CourseCatalogEntries]
                SET [CourseTitle] = [CourseName]
                WHERE [CourseTitle] = ''
                """);

            migrationBuilder.Sql(
                """
                UPDATE [SIRequests]
                SET [RequestedCourseTitle] = [RequestedCourseName]
                WHERE [RequestedCourseTitle] = ''
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedCourseTitle",
                table: "SIRequests");

            migrationBuilder.DropColumn(
                name: "CourseTitle",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CourseTitle",
                table: "CourseCatalogEntries");
        }
    }
}
