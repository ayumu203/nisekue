using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddQuestEquipmentRewardPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "equipment_reward_id",
                schema: "internal",
                table: "quest_reward_summaries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "skipped_reward_player_ids_json",
                schema: "internal",
                table: "quest_reward_summaries",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "equipment_reward_id",
                schema: "internal",
                table: "quest_reward_summaries");

            migrationBuilder.DropColumn(
                name: "skipped_reward_player_ids_json",
                schema: "internal",
                table: "quest_reward_summaries");
        }
    }
}
