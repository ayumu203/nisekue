using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddPlayerMaxLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // レベル上限100の導入に伴い、既存のLv100超プレイヤーをクランプする。
            // Player はコンストラクタでレベルを検証するため、これを行わないと該当プレイヤーの復元時に例外となる。
            // ステータスは players に加算済みの値として保持されるため、レベルを下げても失われない。
            migrationBuilder.Sql("UPDATE internal.players SET level = 100, exp = 0 WHERE level > 100;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // クランプ前のレベルは復元できないため、ロールバック時は何もしない。
        }
    }
}
