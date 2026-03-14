using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace schedule_ders.Migrations
{
    [DbContext(typeof(ScheduleContext))]
    [Migration("20260313201000_AddSiLeaderCustomFields")]
    public partial class AddSiLeaderCustomFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SILeaderCustomFields",
                columns: table => new
                {
                    SILeaderCustomFieldId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SILeaderCustomFields", x => x.SILeaderCustomFieldId);
                });

            migrationBuilder.CreateTable(
                name: "SILeaderCustomValues",
                columns: table => new
                {
                    SILeaderCustomValueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SILeaderID = table.Column<int>(type: "int", nullable: false),
                    SILeaderCustomFieldId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SILeaderCustomValues", x => x.SILeaderCustomValueId);
                    table.ForeignKey(
                        name: "FK_SILeaderCustomValues_SILeaderCustomFields_SILeaderCustomFieldId",
                        column: x => x.SILeaderCustomFieldId,
                        principalTable: "SILeaderCustomFields",
                        principalColumn: "SILeaderCustomFieldId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SILeaderCustomValues_SILeaders_SILeaderID",
                        column: x => x.SILeaderID,
                        principalTable: "SILeaders",
                        principalColumn: "SILeaderID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SILeaderCustomFields_Name",
                table: "SILeaderCustomFields",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SILeaderCustomValues_SILeaderCustomFieldId",
                table: "SILeaderCustomValues",
                column: "SILeaderCustomFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_SILeaderCustomValues_SILeaderID_SILeaderCustomFieldId",
                table: "SILeaderCustomValues",
                columns: new[] { "SILeaderID", "SILeaderCustomFieldId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SILeaderCustomValues");

            migrationBuilder.DropTable(
                name: "SILeaderCustomFields");
        }
    }
}
