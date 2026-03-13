using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddQuestRecruitingRoomOwnerConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH duplicated_recruiting_rooms AS (
                    SELECT
                        id,
                        ROW_NUMBER() OVER (
                            PARTITION BY owner_player_id
                            ORDER BY created_at DESC, id DESC
                        ) AS row_no
                    FROM internal.quest_rooms
                    WHERE status = 1
                )
                UPDATE internal.quest_rooms AS qr
                SET
                    status = 2,
                    close_reason = 2,
                    closed_at = COALESCE(qr.closed_at, CURRENT_TIMESTAMP),
                    version = qr.version + 1
                FROM duplicated_recruiting_rooms AS drr
                WHERE qr.id = drr.id
                  AND drr.row_no > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_quest_rooms_owner_player_id",
                schema: "internal",
                table: "quest_rooms",
                column: "owner_player_id",
                unique: true,
                filter: "status = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_quest_rooms_owner_player_id",
                schema: "internal",
                table: "quest_rooms");
        }
    }
}
