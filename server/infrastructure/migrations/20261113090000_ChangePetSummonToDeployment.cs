using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class ChangePetSummonToDeployment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pet_summons_used",
                schema: "internal",
                table: "quest_run_party_members");

            migrationBuilder.AddColumn<string>(
                name: "pets_json",
                schema: "internal",
                table: "quest_run_party_snapshots",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<bool>(
                name: "pet_summon_used",
                schema: "internal",
                table: "quest_runs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "pet_deployment_json",
                schema: "internal",
                table: "quest_runs",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pet_deployment_json",
                schema: "internal",
                table: "quest_runs");

            migrationBuilder.DropColumn(
                name: "pet_summon_used",
                schema: "internal",
                table: "quest_runs");

            migrationBuilder.DropColumn(
                name: "pets_json",
                schema: "internal",
                table: "quest_run_party_snapshots");

            migrationBuilder.AddColumn<int>(
                name: "pet_summons_used",
                schema: "internal",
                table: "quest_run_party_members",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
