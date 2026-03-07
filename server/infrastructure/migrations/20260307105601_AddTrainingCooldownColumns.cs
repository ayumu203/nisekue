using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddTrainingCooldownColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "training_battle_count",
                schema: "internal",
                table: "players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "training_cooldown_until",
                schema: "internal",
                table: "players",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "training_battle_count",
                schema: "internal",
                table: "players");

            migrationBuilder.DropColumn(
                name: "training_cooldown_until",
                schema: "internal",
                table: "players");
        }
    }
}
