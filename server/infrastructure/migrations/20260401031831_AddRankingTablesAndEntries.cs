using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddRankingTablesAndEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ranking_snapshots",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    interval_hours = table.Column<int>(type: "integer", nullable: false, defaultValue: 6),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ranking_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ranking_entries",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ranking_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    period_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    combat_index_rank = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank_position = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ranking_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_ranking_entries_players_player_id",
                        column: x => x.player_id,
                        principalSchema: "internal",
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ranking_entries_ranking_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "internal",
                        principalTable: "ranking_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ranking_entries_player_id",
                schema: "internal",
                table: "ranking_entries",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_ranking_entries_snapshot_id_ranking_type_period_kind_combat~",
                schema: "internal",
                table: "ranking_entries",
                columns: new[] { "snapshot_id", "ranking_type", "period_kind", "combat_index_rank", "rank_position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ranking_snapshots_snapshot_at",
                schema: "internal",
                table: "ranking_snapshots",
                column: "snapshot_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ranking_entries",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "ranking_snapshots",
                schema: "internal");
        }
    }
}
