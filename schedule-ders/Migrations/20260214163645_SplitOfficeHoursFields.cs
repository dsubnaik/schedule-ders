using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace schedule_ders.Migrations
{
    /// <inheritdoc />
    public partial class SplitOfficeHoursFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OfficeHours",
                table: "Courses",
                newName: "OfficeHoursLocation");

            migrationBuilder.AddColumn<string>(
                name: "OfficeHoursDay",
                table: "Courses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OfficeHoursTime",
                table: "Courses",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE [Courses]
                SET
                    [OfficeHoursDay] = CASE
                        WHEN CHARINDEX(' ', [OfficeHoursLocation]) > 0
                            THEN LEFT([OfficeHoursLocation], CHARINDEX(' ', [OfficeHoursLocation]) - 1)
                        ELSE ''
                    END,
                    [OfficeHoursTime] = CASE
                        WHEN CHARINDEX(' ', [OfficeHoursLocation]) > 0
                            THEN
                                CASE
                                    WHEN CHARINDEX(' ', LTRIM(SUBSTRING([OfficeHoursLocation], CHARINDEX(' ', [OfficeHoursLocation]) + 1, LEN([OfficeHoursLocation])))) > 0
                                        THEN LEFT(
                                            LTRIM(SUBSTRING([OfficeHoursLocation], CHARINDEX(' ', [OfficeHoursLocation]) + 1, LEN([OfficeHoursLocation]))),
                                            CHARINDEX(' ', LTRIM(SUBSTRING([OfficeHoursLocation], CHARINDEX(' ', [OfficeHoursLocation]) + 1, LEN([OfficeHoursLocation])))) - 1
                                        )
                                    ELSE LTRIM(SUBSTRING([OfficeHoursLocation], CHARINDEX(' ', [OfficeHoursLocation]) + 1, LEN([OfficeHoursLocation])))
                                END
                        ELSE ''
                    END,
                    [OfficeHoursLocation] = CASE
                        WHEN CHARINDEX(' ', [OfficeHoursLocation]) > 0
                            THEN
                                CASE
                                    WHEN CHARINDEX(' ', LTRIM(SUBSTRING([OfficeHoursLocation], CHARINDEX(' ', [OfficeHoursLocation]) + 1, LEN([OfficeHoursLocation])))) > 0
                                        THEN LTRIM(SUBSTRING(
                                            LTRIM(SUBSTRING([OfficeHoursLocation], CHARINDEX(' ', [OfficeHoursLocation]) + 1, LEN([OfficeHoursLocation]))),
                                            CHARINDEX(' ', LTRIM(SUBSTRING([OfficeHoursLocation], CHARINDEX(' ', [OfficeHoursLocation]) + 1, LEN([OfficeHoursLocation])))) + 1,
                                            LEN([OfficeHoursLocation])
                                        ))
                                    ELSE ''
                                END
                        ELSE [OfficeHoursLocation]
                    END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [Courses]
                SET [OfficeHoursLocation] =
                    LTRIM(RTRIM(
                        CASE
                            WHEN [OfficeHoursDay] = '' AND [OfficeHoursTime] = ''
                                THEN [OfficeHoursLocation]
                            WHEN [OfficeHoursLocation] = ''
                                THEN [OfficeHoursDay] + ' ' + [OfficeHoursTime]
                            ELSE [OfficeHoursDay] + ' ' + [OfficeHoursTime] + ' ' + [OfficeHoursLocation]
                        END
                    ));
                """);

            migrationBuilder.DropColumn(
                name: "OfficeHoursDay",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "OfficeHoursTime",
                table: "Courses");

            migrationBuilder.RenameColumn(
                name: "OfficeHoursLocation",
                table: "Courses",
                newName: "OfficeHours");
        }
    }
}
