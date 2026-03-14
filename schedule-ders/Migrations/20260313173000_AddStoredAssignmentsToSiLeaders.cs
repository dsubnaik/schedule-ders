using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace schedule_ders.Migrations
{
    [DbContext(typeof(ScheduleContext))]
    [Migration("20260313173000_AddStoredAssignmentsToSiLeaders")]
    public partial class AddStoredAssignmentsToSiLeaders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StoredCourseAssignments",
                table: "SILeaders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoredCourseAssignments",
                table: "SILeaders");
        }
    }
}
