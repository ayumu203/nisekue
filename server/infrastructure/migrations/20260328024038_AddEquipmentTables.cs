using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "armor_player_equipment_id",
                schema: "internal",
                table: "quest_run_party_snapshots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "weapon_player_equipment_id",
                schema: "internal",
                table: "quest_run_party_snapshots",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "player_equipments",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<int>(type: "integer", nullable: false),
                    equipment_type = table.Column<int>(type: "integer", nullable: false),
                    equipment_status = table.Column<int>(type: "integer", nullable: false),
                    durability = table.Column<int>(type: "integer", nullable: false),
                    mastery = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    acquired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_equipments", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_equipments_players_player_id",
                        column: x => x.player_id,
                        principalSchema: "internal",
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_equipments_player_id",
                schema: "internal",
                table: "player_equipments",
                column: "player_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_equipments",
                schema: "internal");

            migrationBuilder.DropColumn(
                name: "armor_player_equipment_id",
                schema: "internal",
                table: "quest_run_party_snapshots");

            migrationBuilder.DropColumn(
                name: "weapon_player_equipment_id",
                schema: "internal",
                table: "quest_run_party_snapshots");
        }
    }
}
