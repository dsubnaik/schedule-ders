using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace schedule_ders.PostgresMigrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseCatalogEntries",
                columns: table => new
                {
                    CourseCatalogEntryID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseCrn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CourseName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CourseTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CourseSection = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseCatalogEntries", x => x.CourseCatalogEntryID);
                });

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    SemesterId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SemesterCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semesters", x => x.SemesterId);
                });

            migrationBuilder.CreateTable(
                name: "SILeaderCustomFields",
                columns: table => new
                {
                    SILeaderCustomFieldId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SILeaderCustomFields", x => x.SILeaderCustomFieldId);
                });

            migrationBuilder.CreateTable(
                name: "SILeaders",
                columns: table => new
                {
                    SILeaderID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ANumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LeaderName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    StoredCourseAssignments = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SILeaders", x => x.SILeaderID);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    CourseID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseCrn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CourseName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CourseTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CourseSection = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CourseMeetingDays = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    CourseMeetingTime = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CourseProfessor = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CourseLeader = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OfficeHoursDay = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OfficeHoursTime = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    OfficeHoursLocation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SemesterId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.CourseID);
                    table.ForeignKey(
                        name: "FK_Courses_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "SemesterId");
                });

            migrationBuilder.CreateTable(
                name: "SILeaderCustomValues",
                columns: table => new
                {
                    SILeaderCustomValueId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SILeaderID = table.Column<int>(type: "integer", nullable: false),
                    SILeaderCustomFieldId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SILeaderCustomValues", x => x.SILeaderCustomValueId);
                    table.ForeignKey(
                        name: "FK_SILeaderCustomValues_SILeaderCustomFields_SILeaderCustomFie~",
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

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Day = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Time = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Location = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CourseID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.SessionID);
                    table.ForeignKey(
                        name: "FK_Sessions_Courses_CourseID",
                        column: x => x.CourseID,
                        principalTable: "Courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SIRequests",
                columns: table => new
                {
                    SIRequestID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseID = table.Column<int>(type: "integer", nullable: true),
                    RequestedCourseName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RequestedCourseTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestedCourseSection = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RequestedCourseProfessor = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ProfessorName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ProfessorEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RequestNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PotentialSiLeaderName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PotentialSiLeaderStatus = table.Column<int>(type: "integer", nullable: false),
                    AdminNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    LastUpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SIRequests", x => x.SIRequestID);
                    table.ForeignKey(
                        name: "FK_SIRequests_Courses_CourseID",
                        column: x => x.CourseID,
                        principalTable: "Courses",
                        principalColumn: "CourseID");
                });

            migrationBuilder.CreateTable(
                name: "StudentFavoriteCourses",
                columns: table => new
                {
                    StudentFavoriteCourseID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CourseID = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "SIRequestLeaderCandidates",
                columns: table => new
                {
                    SIRequestLeaderCandidateID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SIRequestID = table.Column<int>(type: "integer", nullable: false),
                    CandidateName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseCatalogEntries_CourseCrn",
                table: "CourseCatalogEntries",
                column: "CourseCrn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_SemesterId",
                table: "Courses",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Semesters_SemesterCode",
                table: "Semesters",
                column: "SemesterCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_CourseID",
                table: "Sessions",
                column: "CourseID");

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

            migrationBuilder.CreateIndex(
                name: "IX_SILeaders_ANumber",
                table: "SILeaders",
                column: "ANumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SIRequestLeaderCandidates_SIRequestID",
                table: "SIRequestLeaderCandidates",
                column: "SIRequestID");

            migrationBuilder.CreateIndex(
                name: "IX_SIRequests_CourseID",
                table: "SIRequests",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFavoriteCourses_CourseID",
                table: "StudentFavoriteCourses",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFavoriteCourses_UserId_CourseID",
                table: "StudentFavoriteCourses",
                columns: new[] { "UserId", "CourseID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CourseCatalogEntries");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "SILeaderCustomValues");

            migrationBuilder.DropTable(
                name: "SIRequestLeaderCandidates");

            migrationBuilder.DropTable(
                name: "StudentFavoriteCourses");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "SILeaderCustomFields");

            migrationBuilder.DropTable(
                name: "SILeaders");

            migrationBuilder.DropTable(
                name: "SIRequests");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Semesters");
        }
    }
}
