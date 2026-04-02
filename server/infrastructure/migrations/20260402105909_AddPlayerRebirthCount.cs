using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddPlayerRebirthCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "rebirth_count",
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
                name: "rebirth_count",
                schema: "internal",
                table: "players");
        }
    }
}
