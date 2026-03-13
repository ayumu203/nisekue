using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using server.infrastructure;

#nullable disable

namespace server.infrastructure.migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260312093000_AddQuestRoomVersionConcurrency")]
    public partial class AddQuestRoomVersionConcurrency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "internal",
                table: "quest_rooms",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                schema: "internal",
                table: "quest_rooms");
        }
    }
}
