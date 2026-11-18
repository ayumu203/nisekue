using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageIsDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "internal",
                table: "chat_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "internal",
                table: "global_chat_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "internal",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "internal",
                table: "global_chat_messages");
        }
    }
}
