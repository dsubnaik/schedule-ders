using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace schedule_ders.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaderCandidateTrackingPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SIRequestLeaderCandidates",
                columns: table => new
                {
                    SIRequestLeaderCandidateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SIRequestID = table.Column<int>(type: "int", nullable: false),
                    CandidateName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SIRequestLeaderCandidates", x => x.SIRequestLeaderCandidateID);
                    table.ForeignKey(
                        name: "FK_SIRequestLeaderCandidates_SIRequests_SIRequestID",
                        column: x => x.SIRequestID,
                        principalTable: "SIRequests",
                        principalColumn: "SIRequestID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SIRequestLeaderCandidates_SIRequestID",
                table: "SIRequestLeaderCandidates",
                column: "SIRequestID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SIRequestLeaderCandidates");
        }
    }
}
