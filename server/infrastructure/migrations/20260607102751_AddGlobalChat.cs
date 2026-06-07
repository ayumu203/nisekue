using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddGlobalChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "global_chat_messages",
                schema: "internal",
                columns: table => new
                {
                    chat_id = table.Column<int>(type: "integer", nullable: false),
                    sender_type = table.Column<int>(type: "integer", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: true),
                    message = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_chat_messages", x => x.chat_id);
                });

            migrationBuilder.CreateTable(
                name: "global_chat_rooms",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_chat_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_chat_rooms", x => x.id);
                });

            migrationBuilder.Sql("INSERT INTO internal.global_chat_rooms(id, last_chat_id) VALUES ('00000000-0000-0000-0000-000000000001', 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "global_chat_messages",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "global_chat_rooms",
                schema: "internal");
        }
    }
}
