using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddPlayerPets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "pet_defense",
                schema: "internal",
                table: "quest_run_party_snapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pet_enemy_definition_id",
                schema: "internal",
                table: "quest_run_party_snapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pet_intelligence",
                schema: "internal",
                table: "quest_run_party_snapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pet_luck",
                schema: "internal",
                table: "quest_run_party_snapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pet_max_hp",
                schema: "internal",
                table: "quest_run_party_snapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pet_max_mp",
                schema: "internal",
                table: "quest_run_party_snapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pet_speed",
                schema: "internal",
                table: "quest_run_party_snapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pet_strength",
                schema: "internal",
                table: "quest_run_party_snapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pet_summons_used",
                schema: "internal",
                table: "quest_run_party_members",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_captured",
                schema: "internal",
                table: "quest_run_enemies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "player_pets",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enemy_definition_id = table.Column<int>(type: "integer", nullable: false),
                    bonus_max_hp = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bonus_max_mp = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bonus_strength = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bonus_defense = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bonus_intelligence = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bonus_luck = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bonus_speed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_pets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_pets_player_id",
                schema: "internal",
                table: "player_pets",
                column: "player_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_pets",
                schema: "internal");

            migrationBuilder.DropColumn(
                name: "pet_defense",
                schema: "internal",
                table: "quest_run_party_snapshots");

            migrationBuilder.DropColumn(
                name: "pet_enemy_definition_id",
                schema: "internal",
                table: "quest_run_party_snapshots");

            migrationBuilder.DropColumn(
                name: "pet_intelligence",
                schema: "internal",
                table: "quest_run_party_snapshots");

            migrationBuilder.DropColumn(
                name: "pet_luck",
                schema: "internal",
                table: "quest_run_party_snapshots");

            migrationBuilder.DropColumn(
                name: "pet_max_hp",
                schema: "internal",
                table: "quest_run_party_snapshots");

            migrationBuilder.DropColumn(
                name: "pet_max_mp",
                schema: "internal",
                table: "quest_run_party_snapshots");

            migrationBuilder.DropColumn(
                name: "pet_speed",
                schema: "internal",
                table: "quest_run_party_snapshots");

            migrationBuilder.DropColumn(
                name: "pet_strength",
                schema: "internal",
                table: "quest_run_party_snapshots");

            migrationBuilder.DropColumn(
                name: "pet_summons_used",
                schema: "internal",
                table: "quest_run_party_members");

            migrationBuilder.DropColumn(
                name: "is_captured",
                schema: "internal",
                table: "quest_run_enemies");
        }
    }
}
