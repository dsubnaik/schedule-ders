using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace schedule_ders.Migrations
{
    /// <inheritdoc />
    public partial class BackfillLeaderCandidatesFromLegacyField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH CandidateTokens AS (
                    SELECT
                        r.SIRequestID,
                        LTRIM(RTRIM(s.[value])) AS CandidateName
                    FROM SIRequests r
                    CROSS APPLY STRING_SPLIT(
                        REPLACE(REPLACE(REPLACE(ISNULL(r.PotentialSiLeaderName, ''), CHAR(13), ','), CHAR(10), ','), ';', ','),
                        ','
                    ) s
                    WHERE LEN(LTRIM(RTRIM(s.[value]))) > 0
                )
                INSERT INTO SIRequestLeaderCandidates (SIRequestID, CandidateName, Status, LastUpdatedAtUtc)
                SELECT ct.SIRequestID, ct.CandidateName, 0, NULL
                FROM CandidateTokens ct
                LEFT JOIN SIRequestLeaderCandidates existing
                    ON existing.SIRequestID = ct.SIRequestID
                    AND LOWER(existing.CandidateName) = LOWER(ct.CandidateName)
                WHERE existing.SIRequestLeaderCandidateID IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
