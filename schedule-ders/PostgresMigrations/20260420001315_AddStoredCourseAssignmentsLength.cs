using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace schedule_ders.PostgresMigrations
{
    /// <inheritdoc />
    public partial class AddStoredCourseAssignmentsLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StoredCourseAssignments",
                table: "SILeaders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StoredCourseAssignments",
                table: "SILeaders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);
        }
    }
}
