using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddPetBattle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "pet_battle_cooldown_until",
                schema: "internal",
                table: "players",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "pet_battle_rooms",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opponent_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    close_reason = table.Column<int>(type: "integer", nullable: true),
                    slots_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_battle_rooms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pet_battle_runs",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opponent_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    current_turn_no = table.Column<int>(type: "integer", nullable: false),
                    action_deadline_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_resolved_turn_no = table.Column<int>(type: "integer", nullable: true),
                    owner_snapshots_json = table.Column<string>(type: "jsonb", nullable: false),
                    opponent_snapshots_json = table.Column<string>(type: "jsonb", nullable: false),
                    owner_members_json = table.Column<string>(type: "jsonb", nullable: false),
                    opponent_members_json = table.Column<string>(type: "jsonb", nullable: false),
                    last_turn_results_json = table.Column<string>(type: "jsonb", nullable: true),
                    winner_player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_battle_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_pet_battle_stats",
                schema: "internal",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false, defaultValue: 1000),
                    wins = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    losses = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_battles = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_pet_battle_stats", x => x.player_id);
                    table.ForeignKey(
                        name: "FK_player_pet_battle_stats_players_player_id",
                        column: x => x.player_id,
                        principalSchema: "internal",
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pet_battle_turn_commands",
                schema: "internal",
                columns: table => new
                {
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    turn_no = table.Column<int>(type: "integer", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_kind = table.Column<int>(type: "integer", nullable: false),
                    move_id = table.Column<int>(type: "integer", nullable: true),
                    target_row = table.Column<int>(type: "integer", nullable: true),
                    target_column = table.Column<int>(type: "integer", nullable: true),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_auto_submitted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_battle_turn_commands", x => new { x.run_id, x.turn_no, x.participant_id });
                    table.ForeignKey(
                        name: "FK_pet_battle_turn_commands_pet_battle_runs_run_id",
                        column: x => x.run_id,
                        principalSchema: "internal",
                        principalTable: "pet_battle_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pet_battle_runs_room_id",
                schema: "internal",
                table: "pet_battle_runs",
                column: "room_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pet_battle_rooms",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "pet_battle_turn_commands",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "player_pet_battle_stats",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "pet_battle_runs",
                schema: "internal");

            migrationBuilder.DropColumn(
                name: "pet_battle_cooldown_until",
                schema: "internal",
                table: "players");
        }
    }
}
