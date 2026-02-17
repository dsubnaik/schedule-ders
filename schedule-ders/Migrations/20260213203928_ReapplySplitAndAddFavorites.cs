using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace schedule_ders.Migrations
{
    /// <inheritdoc />
    public partial class ReapplySplitAndAddFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SIRequests_Courses_CourseID",
                table: "SIRequests");

            migrationBuilder.AlterColumn<int>(
                name: "CourseID",
                table: "SIRequests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "RequestedCourseName",
                table: "SIRequests",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestedCourseProfessor",
                table: "SIRequests",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestedCourseSection",
                table: "SIRequests",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CourseMeetingDays",
                table: "Courses",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CourseMeetingTime",
                table: "Courses",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE [Courses]
                SET
                    [CourseMeetingDays] = CASE
                        WHEN CHARINDEX(' ', [CourseMeetingTimes]) > 0
                            THEN LEFT([CourseMeetingTimes], CHARINDEX(' ', [CourseMeetingTimes]) - 1)
                        ELSE [CourseMeetingTimes]
                    END,
                    [CourseMeetingTime] = CASE
                        WHEN CHARINDEX(' ', [CourseMeetingTimes]) > 0
                            THEN LTRIM(SUBSTRING([CourseMeetingTimes], CHARINDEX(' ', [CourseMeetingTimes]) + 1, LEN([CourseMeetingTimes])))
                        ELSE ''
                    END;
                """);

            migrationBuilder.DropColumn(
                name: "CourseMeetingTimes",
                table: "Courses");

            migrationBuilder.CreateTable(
                name: "StudentFavoriteCourses",
                columns: table => new
                {
                    StudentFavoriteCourseID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CourseID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentFavoriteCourses", x => x.StudentFavoriteCourseID);
                    table.ForeignKey(
                        name: "FK_StudentFavoriteCourses_Courses_CourseID",
                        column: x => x.CourseID,
                        principalTable: "Courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentFavoriteCourses_CourseID",
                table: "StudentFavoriteCourses",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFavoriteCourses_UserId_CourseID",
                table: "StudentFavoriteCourses",
                columns: new[] { "UserId", "CourseID" },
                unique: true);

            migrationBuilder.Sql(
                """
                UPDATE [r]
                SET
                    [r].[RequestedCourseName] = ISNULL([c].[CourseName], ''),
                    [r].[RequestedCourseSection] = ISNULL([c].[CourseSection], ''),
                    [r].[RequestedCourseProfessor] = ISNULL([c].[CourseProfessor], '')
                FROM [SIRequests] [r]
                LEFT JOIN [Courses] [c] ON [r].[CourseID] = [c].[CourseID];
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_SIRequests_Courses_CourseID",
                table: "SIRequests",
                column: "CourseID",
                principalTable: "Courses",
                principalColumn: "CourseID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SIRequests_Courses_CourseID",
                table: "SIRequests");

            migrationBuilder.DropTable(
                name: "StudentFavoriteCourses");

            migrationBuilder.DropColumn(
                name: "RequestedCourseName",
                table: "SIRequests");

            migrationBuilder.DropColumn(
                name: "RequestedCourseProfessor",
                table: "SIRequests");

            migrationBuilder.DropColumn(
                name: "RequestedCourseSection",
                table: "SIRequests");

            migrationBuilder.DropColumn(
                name: "CourseMeetingDays",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CourseMeetingTime",
                table: "Courses");

            migrationBuilder.AlterColumn<int>(
                name: "CourseID",
                table: "SIRequests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourseMeetingTimes",
                table: "Courses",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE [Courses]
                SET [CourseMeetingTimes] =
                    LTRIM(RTRIM(
                        CASE
                            WHEN [CourseMeetingTime] = ''
                                THEN [CourseMeetingDays]
                            ELSE [CourseMeetingDays] + ' ' + [CourseMeetingTime]
                        END
                    ));
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_SIRequests_Courses_CourseID",
                table: "SIRequests",
                column: "CourseID",
                principalTable: "Courses",
                principalColumn: "CourseID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
