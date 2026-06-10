using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddPetBattleConcurrencyAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "internal",
                table: "pet_battle_runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_pet_battle_runs_owner_status",
                schema: "internal",
                table: "pet_battle_runs",
                columns: new[] { "owner_player_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_pet_battle_runs_status_deadline",
                schema: "internal",
                table: "pet_battle_runs",
                columns: new[] { "status", "action_deadline_at" });

            migrationBuilder.CreateIndex(
                name: "ix_pet_battle_rooms_owner_status",
                schema: "internal",
                table: "pet_battle_rooms",
                columns: new[] { "owner_player_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_pet_battle_runs_owner_status",
                schema: "internal",
                table: "pet_battle_runs");

            migrationBuilder.DropIndex(
                name: "ix_pet_battle_runs_status_deadline",
                schema: "internal",
                table: "pet_battle_runs");

            migrationBuilder.DropIndex(
                name: "ix_pet_battle_rooms_owner_status",
                schema: "internal",
                table: "pet_battle_rooms");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "internal",
                table: "pet_battle_runs");
        }
    }
}
