using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddQuestRoomJoinRestrictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "min_required_level",
                schema: "internal",
                table: "quest_rooms",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "quest_room_allowed_players",
                schema: "internal",
                columns: table => new
                {
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest_room_allowed_players", x => new { x.room_id, x.player_id });
                    table.ForeignKey(
                        name: "FK_quest_room_allowed_players_players_player_id",
                        column: x => x.player_id,
                        principalSchema: "internal",
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quest_room_allowed_players_quest_rooms_room_id",
                        column: x => x.room_id,
                        principalSchema: "internal",
                        principalTable: "quest_rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quest_room_allowed_players_player_id",
                schema: "internal",
                table: "quest_room_allowed_players",
                column: "player_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quest_room_allowed_players",
                schema: "internal");

            migrationBuilder.DropColumn(
                name: "min_required_level",
                schema: "internal",
                table: "quest_rooms");
        }
    }
}
