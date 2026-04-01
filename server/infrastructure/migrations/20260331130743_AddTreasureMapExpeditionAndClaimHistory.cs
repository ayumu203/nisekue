using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddTreasureMapExpeditionAndClaimHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "treasure_map_claim_histories",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expedition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reward_summary_json = table.Column<string>(type: "jsonb", nullable: false),
                    claimed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treasure_map_claim_histories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "treasure_map_expeditions",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    map_id = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    reward_summary_json = table.Column<string>(type: "jsonb", nullable: true),
                    reward_claimed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treasure_map_expeditions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_treasure_map_claim_histories_expedition_id",
                schema: "internal",
                table: "treasure_map_claim_histories",
                column: "expedition_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_treasure_map_claim_histories_player_id",
                schema: "internal",
                table: "treasure_map_claim_histories",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_treasure_map_expeditions_player_id",
                schema: "internal",
                table: "treasure_map_expeditions",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_treasure_map_expeditions_player_id_status",
                schema: "internal",
                table: "treasure_map_expeditions",
                columns: new[] { "player_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "treasure_map_claim_histories",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "treasure_map_expeditions",
                schema: "internal");
        }
    }
}
