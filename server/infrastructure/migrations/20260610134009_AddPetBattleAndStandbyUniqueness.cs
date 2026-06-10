using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddPetBattleAndStandbyUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH duplicated_standby_pets AS (
                    SELECT
                        id,
                        ROW_NUMBER() OVER (
                            PARTITION BY player_id
                            ORDER BY updated_at DESC, captured_at DESC, id DESC
                        ) AS row_no
                    FROM internal.player_pets
                    WHERE is_standby = TRUE
                )
                UPDATE internal.player_pets AS pp
                SET
                    is_standby = FALSE,
                    updated_at = CURRENT_TIMESTAMP
                FROM duplicated_standby_pets AS dsp
                WHERE pp.id = dsp.id
                  AND dsp.row_no > 1;
                """);

            migrationBuilder.Sql("""
                WITH duplicated_waiting_rooms AS (
                    SELECT
                        id,
                        ROW_NUMBER() OVER (
                            PARTITION BY owner_player_id
                            ORDER BY created_at DESC, id DESC
                        ) AS row_no
                    FROM internal.pet_battle_rooms
                    WHERE status = 1
                )
                UPDATE internal.pet_battle_rooms AS pbr
                SET
                    status = 2,
                    close_reason = 2,
                    closed_at = COALESCE(pbr.closed_at, CURRENT_TIMESTAMP),
                    version = pbr.version + 1
                FROM duplicated_waiting_rooms AS dwr
                WHERE pbr.id = dwr.id
                  AND dwr.row_no > 1;
                """);

            migrationBuilder.Sql("""
                WITH duplicated_active_runs AS (
                    SELECT
                        id,
                        ROW_NUMBER() OVER (
                            PARTITION BY owner_player_id
                            ORDER BY started_at DESC, id DESC
                        ) AS row_no
                    FROM internal.pet_battle_runs
                    WHERE status = 1
                )
                UPDATE internal.pet_battle_runs AS pbr
                SET
                    status = 4,
                    ended_at = COALESCE(pbr.ended_at, CURRENT_TIMESTAMP),
                    version = pbr.version + 1
                FROM duplicated_active_runs AS dar
                WHERE pbr.id = dar.id
                  AND dar.row_no > 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_player_pets_player_id",
                schema: "internal",
                table: "player_pets");

            migrationBuilder.CreateIndex(
                name: "ix_player_pets_standby_player",
                schema: "internal",
                table: "player_pets",
                column: "player_id",
                unique: true,
                filter: "is_standby = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_pet_battle_runs_in_progress_owner",
                schema: "internal",
                table: "pet_battle_runs",
                column: "owner_player_id",
                unique: true,
                filter: "status = 1");

            migrationBuilder.CreateIndex(
                name: "ix_pet_battle_rooms_waiting_owner",
                schema: "internal",
                table: "pet_battle_rooms",
                column: "owner_player_id",
                unique: true,
                filter: "status = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_player_pets_standby_player",
                schema: "internal",
                table: "player_pets");

            migrationBuilder.DropIndex(
                name: "ix_pet_battle_runs_in_progress_owner",
                schema: "internal",
                table: "pet_battle_runs");

            migrationBuilder.DropIndex(
                name: "ix_pet_battle_rooms_waiting_owner",
                schema: "internal",
                table: "pet_battle_rooms");

            migrationBuilder.CreateIndex(
                name: "IX_player_pets_player_id",
                schema: "internal",
                table: "player_pets",
                column: "player_id");
        }
    }
}
