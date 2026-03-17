using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddPlayerJobLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "level",
                schema: "internal",
                table: "players",
                newName: "job_level");

            migrationBuilder.RenameColumn(
                name: "exp",
                schema: "internal",
                table: "players",
                newName: "job_exp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "job_level",
                schema: "internal",
                table: "players",
                newName: "level");

            migrationBuilder.RenameColumn(
                name: "job_exp",
                schema: "internal",
                table: "players",
                newName: "exp");
        }
    }
}
