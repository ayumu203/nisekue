using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class MakeChatMessageAlertsIndependent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_chat_message_alerts_chat_messages_owner_id_chat_id",
                schema: "internal",
                table: "chat_message_alerts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_chat_message_alerts_chat_messages_owner_id_chat_id",
                schema: "internal",
                table: "chat_message_alerts",
                columns: new[] { "owner_id", "chat_id" },
                principalSchema: "internal",
                principalTable: "chat_messages",
                principalColumns: new[] { "owner_id", "chat_id" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
