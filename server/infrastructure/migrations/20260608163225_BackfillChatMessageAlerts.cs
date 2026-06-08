using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class BackfillChatMessageAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // chat_message_alerts テーブル追加前に存在していたメッセージは
            // アラート行が作成されていないため、既読済みとして一括登録する
            migrationBuilder.Sql(@"
                INSERT INTO internal.chat_message_alerts (owner_id, chat_id, is_alerted)
                SELECT m.owner_id, m.chat_id, true
                FROM internal.chat_messages m
                WHERE NOT EXISTS (
                    SELECT 1 FROM internal.chat_message_alerts a
                    WHERE a.owner_id = m.owner_id AND a.chat_id = m.chat_id
                )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
