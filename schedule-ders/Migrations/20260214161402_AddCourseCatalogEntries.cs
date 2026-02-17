using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace schedule_ders.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseCatalogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseCatalogEntries",
                columns: table => new
                {
                    CourseCatalogEntryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseCrn = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CourseSection = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseCatalogEntries", x => x.CourseCatalogEntryID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseCatalogEntries_CourseCrn",
                table: "CourseCatalogEntries",
                column: "CourseCrn",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO [CourseCatalogEntries] ([CourseCrn], [CourseName], [CourseSection])
                SELECT DISTINCT [CourseCrn], [CourseName], [CourseSection]
                FROM [Courses]
                WHERE [CourseCrn] <> ''
                AND NOT EXISTS (
                    SELECT 1
                    FROM [CourseCatalogEntries] [c]
                    WHERE [c].[CourseCrn] = [Courses].[CourseCrn]
                );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO [CourseCatalogEntries] ([CourseCrn], [CourseName], [CourseSection])
                SELECT [v].[CourseCrn], [v].[CourseName], [v].[CourseSection]
                FROM (VALUES
                    ('21001', 'ACCT 2301', '002'),
                    ('21002', 'ACCT 2302', '002'),
                    ('21003', 'BIOL 2401', '001'),
                    ('21004', 'BIOL 2401', '002'),
                    ('21005', 'BIOL 2416', '001'),
                    ('21006', 'BIOL 2416', '002'),
                    ('21007', 'BIOL 3428', '001'),
                    ('21008', 'CHEM 1411', '001'),
                    ('21009', 'CHEM 1411', '002'),
                    ('21010', 'CHEM 1411', '003'),
                    ('21011', 'CHEM 1411', '004'),
                    ('21012', 'CHEM 1412', '002')
                ) AS [v]([CourseCrn], [CourseName], [CourseSection])
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [CourseCatalogEntries] [c]
                    WHERE [c].[CourseCrn] = [v].[CourseCrn]
                );
                """);

            migrationBuilder.Sql(
                """
                WITH d AS (
                    SELECT
                        [CourseCatalogEntryID],
                        ROW_NUMBER() OVER (PARTITION BY [CourseCrn] ORDER BY [CourseCatalogEntryID]) AS [rn]
                    FROM [CourseCatalogEntries]
                )
                DELETE FROM d WHERE [rn] > 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseCatalogEntries");
        }
    }
}
