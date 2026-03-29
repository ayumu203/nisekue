using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddThreadTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "threads",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    body = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_replied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_threads", x => x.id);
                    table.ForeignKey(
                        name: "FK_threads_players_author_player_id",
                        column: x => x.author_player_id,
                        principalSchema: "internal",
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "thread_replies",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    thread_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_thread_replies", x => x.id);
                    table.ForeignKey(
                        name: "FK_thread_replies_players_author_player_id",
                        column: x => x.author_player_id,
                        principalSchema: "internal",
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_thread_replies_threads_thread_id",
                        column: x => x.thread_id,
                        principalSchema: "internal",
                        principalTable: "threads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_thread_replies_author_player_id",
                schema: "internal",
                table: "thread_replies",
                column: "author_player_id");

            migrationBuilder.CreateIndex(
                name: "IX_thread_replies_thread_id",
                schema: "internal",
                table: "thread_replies",
                column: "thread_id");

            migrationBuilder.CreateIndex(
                name: "IX_threads_author_player_id",
                schema: "internal",
                table: "threads",
                column: "author_player_id");

            migrationBuilder.CreateIndex(
                name: "IX_threads_created_at",
                schema: "internal",
                table: "threads",
                column: "created_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "thread_replies",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "threads",
                schema: "internal");
        }
    }
}
