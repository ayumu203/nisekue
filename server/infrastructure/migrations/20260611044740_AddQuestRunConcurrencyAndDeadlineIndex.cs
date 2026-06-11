using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddQuestRunConcurrencyAndDeadlineIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "internal",
                table: "quest_runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_quest_runs_status_deadline",
                schema: "internal",
                table: "quest_runs",
                columns: new[] { "status", "action_deadline_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_quest_runs_status_deadline",
                schema: "internal",
                table: "quest_runs");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "internal",
                table: "quest_runs");
        }
    }
}
