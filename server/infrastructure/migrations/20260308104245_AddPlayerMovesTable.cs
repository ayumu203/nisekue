using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddPlayerMovesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_moves",
                schema: "internal",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    move_id_1 = table.Column<int>(type: "integer", nullable: true),
                    move_id_2 = table.Column<int>(type: "integer", nullable: true),
                    move_id_3 = table.Column<int>(type: "integer", nullable: true),
                    move_id_4 = table.Column<int>(type: "integer", nullable: true),
                    move_id_5 = table.Column<int>(type: "integer", nullable: true),
                    move_id_6 = table.Column<int>(type: "integer", nullable: true),
                    move_id_7 = table.Column<int>(type: "integer", nullable: true),
                    move_id_8 = table.Column<int>(type: "integer", nullable: true),
                    move_id_9 = table.Column<int>(type: "integer", nullable: true),
                    move_id_10 = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_moves", x => x.player_id);
                    table.ForeignKey(
                        name: "FK_player_moves_players_player_id",
                        column: x => x.player_id,
                        principalSchema: "internal",
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_moves",
                schema: "internal");
        }
    }
}
