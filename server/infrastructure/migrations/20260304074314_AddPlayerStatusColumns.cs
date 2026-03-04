using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddPlayerStatusColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "defense",
                schema: "internal",
                table: "players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "exp",
                schema: "internal",
                table: "players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "intelligence",
                schema: "internal",
                table: "players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "level",
                schema: "internal",
                table: "players",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "luck",
                schema: "internal",
                table: "players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "max_hp",
                schema: "internal",
                table: "players",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "max_mp",
                schema: "internal",
                table: "players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "speed",
                schema: "internal",
                table: "players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "strength",
                schema: "internal",
                table: "players",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "defense",
                schema: "internal",
                table: "players");

            migrationBuilder.DropColumn(
                name: "exp",
                schema: "internal",
                table: "players");

            migrationBuilder.DropColumn(
                name: "intelligence",
                schema: "internal",
                table: "players");

            migrationBuilder.DropColumn(
                name: "level",
                schema: "internal",
                table: "players");

            migrationBuilder.DropColumn(
                name: "luck",
                schema: "internal",
                table: "players");

            migrationBuilder.DropColumn(
                name: "max_hp",
                schema: "internal",
                table: "players");

            migrationBuilder.DropColumn(
                name: "max_mp",
                schema: "internal",
                table: "players");

            migrationBuilder.DropColumn(
                name: "speed",
                schema: "internal",
                table: "players");

            migrationBuilder.DropColumn(
                name: "strength",
                schema: "internal",
                table: "players");
        }
    }
}
