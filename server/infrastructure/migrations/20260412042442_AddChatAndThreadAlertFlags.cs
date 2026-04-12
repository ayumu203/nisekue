using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddChatAndThreadAlertFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_author_alerted",
                schema: "internal",
                table: "thread_replies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_alerted",
                schema: "internal",
                table: "chat_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE internal.thread_replies
                SET is_author_alerted = TRUE;
                """);

            migrationBuilder.Sql("""
                UPDATE internal.chat_messages
                SET is_alerted = TRUE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_author_alerted",
                schema: "internal",
                table: "thread_replies");

            migrationBuilder.DropColumn(
                name: "is_alerted",
                schema: "internal",
                table: "chat_messages");
        }
    }
}
