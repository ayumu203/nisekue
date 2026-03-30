using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddQuestRewardGold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "gold",
                schema: "internal",
                table: "quest_reward_summaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gold",
                schema: "internal",
                table: "quest_reward_summaries");
        }
    }
}
