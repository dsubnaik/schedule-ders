using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace schedule_ders.Migrations
{
    [DbContext(typeof(ScheduleContext))]
    [Migration("20260309123000_AddSILeadersManagement")]
    public partial class AddSILeadersManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SILeaders",
                columns: table => new
                {
                    SILeaderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ANumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LeaderName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SILeaders", x => x.SILeaderID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SILeaders_ANumber",
                table: "SILeaders",
                column: "ANumber",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SILeaders");
        }
    }
}
