using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddQuestRecruitingRoomOwnerConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_quest_rooms_owner_player_id",
                schema: "internal",
                table: "quest_rooms",
                column: "owner_player_id",
                unique: true,
                filter: "status = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_quest_rooms_owner_player_id",
                schema: "internal",
                table: "quest_rooms");
        }
    }
}
