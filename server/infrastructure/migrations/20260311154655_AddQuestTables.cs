using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddQuestTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quest_floor_traps",
                schema: "internal",
                columns: table => new
                {
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trap_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    move_id = table.Column<int>(type: "integer", nullable: false),
                    expires_after_floor_no = table.Column<int>(type: "integer", nullable: false),
                    is_triggered = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest_floor_traps", x => new { x.run_id, x.trap_id });
                });

            migrationBuilder.CreateTable(
                name: "quest_reward_summaries",
                schema: "internal",
                columns: table => new
                {
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exp = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest_reward_summaries", x => x.run_id);
                });

            migrationBuilder.CreateTable(
                name: "quest_room_participants",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_type = table.Column<int>(type: "integer", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    npc_template_id = table.Column<int>(type: "integer", nullable: true),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    battle_row = table.Column<int>(type: "integer", nullable: false),
                    battle_column = table.Column<int>(type: "integer", nullable: false),
                    participant_status = table.Column<int>(type: "integer", nullable: false),
                    is_owner = table.Column<bool>(type: "boolean", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    left_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest_room_participants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quest_rooms",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage_id = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    close_reason = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest_rooms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quest_run_enemies",
                schema: "internal",
                columns: table => new
                {
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enemy_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    floor_no = table.Column<int>(type: "integer", nullable: false),
                    enemy_definition_id = table.Column<int>(type: "integer", nullable: false),
                    battle_row = table.Column<int>(type: "integer", nullable: false),
                    battle_column = table.Column<int>(type: "integer", nullable: false),
                    current_hp = table.Column<int>(type: "integer", nullable: false),
                    current_mp = table.Column<int>(type: "integer", nullable: false),
                    is_dead = table.Column<bool>(type: "boolean", nullable: false),
                    active_effects_json = table.Column<string>(type: "jsonb", nullable: false),
                    derived_parameters_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest_run_enemies", x => new { x.run_id, x.enemy_instance_id });
                });

            migrationBuilder.CreateTable(
                name: "quest_run_party_members",
                schema: "internal",
                columns: table => new
                {
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_hp = table.Column<int>(type: "integer", nullable: false),
                    current_mp = table.Column<int>(type: "integer", nullable: false),
                    is_dead = table.Column<bool>(type: "boolean", nullable: false),
                    can_act_from_turn = table.Column<int>(type: "integer", nullable: false),
                    action_mode = table.Column<int>(type: "integer", nullable: false),
                    has_left_quest = table.Column<bool>(type: "boolean", nullable: false),
                    is_manual_control_requested = table.Column<bool>(type: "boolean", nullable: false),
                    active_effects_json = table.Column<string>(type: "jsonb", nullable: false),
                    derived_parameters_json = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest_run_party_members", x => new { x.run_id, x.participant_id });
                });

            migrationBuilder.CreateTable(
                name: "quest_run_party_snapshots",
                schema: "internal",
                columns: table => new
                {
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_type = table.Column<int>(type: "integer", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    image_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    job = table.Column<int>(type: "integer", nullable: false),
                    start_row = table.Column<int>(type: "integer", nullable: false),
                    start_column = table.Column<int>(type: "integer", nullable: false),
                    max_hp = table.Column<int>(type: "integer", nullable: false),
                    max_mp = table.Column<int>(type: "integer", nullable: false),
                    strength = table.Column<int>(type: "integer", nullable: false),
                    defense = table.Column<int>(type: "integer", nullable: false),
                    intelligence = table.Column<int>(type: "integer", nullable: false),
                    luck = table.Column<int>(type: "integer", nullable: false),
                    speed = table.Column<int>(type: "integer", nullable: false),
                    move_set_json = table.Column<string>(type: "jsonb", nullable: false),
                    initial_action_mode = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest_run_party_snapshots", x => new { x.run_id, x.participant_id });
                });

            migrationBuilder.CreateTable(
                name: "quest_runs",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    current_floor_no = table.Column<int>(type: "integer", nullable: false),
                    current_turn_no = table.Column<int>(type: "integer", nullable: false),
                    action_deadline_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_resolved_turn_no = table.Column<int>(type: "integer", nullable: true),
                    last_turn_results_json = table.Column<string>(type: "jsonb", nullable: true),
                    chat_messages_json = table.Column<string>(type: "jsonb", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quest_turn_commands",
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
                    table.PrimaryKey("PK_quest_turn_commands", x => new { x.run_id, x.turn_no, x.participant_id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_quest_room_participants_room_id_battle_row_battle_column",
                schema: "internal",
                table: "quest_room_participants",
                columns: new[] { "room_id", "battle_row", "battle_column" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quest_runs_room_id",
                schema: "internal",
                table: "quest_runs",
                column: "room_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quest_floor_traps",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "quest_reward_summaries",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "quest_room_participants",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "quest_rooms",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "quest_run_enemies",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "quest_run_party_members",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "quest_run_party_snapshots",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "quest_runs",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "quest_turn_commands",
                schema: "internal");
        }
    }
}
