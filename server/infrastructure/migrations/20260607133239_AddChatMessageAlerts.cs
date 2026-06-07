using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_message_alerts",
                schema: "internal",
                columns: table => new
                {
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_id = table.Column<int>(type: "integer", nullable: false),
                    is_alerted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_message_alerts", x => new { x.owner_id, x.chat_id });
                    table.ForeignKey(
                        name: "FK_chat_message_alerts_chat_messages_owner_id_chat_id",
                        columns: x => new { x.owner_id, x.chat_id },
                        principalSchema: "internal",
                        principalTable: "chat_messages",
                        principalColumns: new[] { "owner_id", "chat_id" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_message_alerts",
                schema: "internal");
        }
    }
}
