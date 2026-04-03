using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace schedule_ders.PostgresMigrations
{
    [DbContext(typeof(ScheduleContext))]
    [Migration("20260403183000_AddCandidateANumberToLeaderCandidates")]
    public partial class AddCandidateANumberToLeaderCandidates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CandidateANumber",
                table: "SIRequestLeaderCandidates",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CandidateANumber",
                table: "SIRequestLeaderCandidates");
        }
    }
}
