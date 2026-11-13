using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "announcements",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_announcements", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_announcements_date_created_at",
                schema: "internal",
                table: "announcements",
                columns: new[] { "date", "created_at" });

            // これまで client/locale/announcement/Announcement.json に書いていたお知らせを移行する
            migrationBuilder.InsertData(
                schema: "internal",
                table: "announcements",
                columns: new[] { "id", "date", "title", "body", "created_at" },
                columnTypes: new[] { "character varying(100)", "date", "character varying(100)", "character varying(2000)", "timestamp with time zone" },
                values: new object[,]
                {
                    {
                        "2026-11-06-server-maintenance",
                        new DateOnly(2026, 11, 4),
                        "【重要】11/6(金) 11:00〜15:00 メンテナンスのお知らせ",
                        "11/6(金) 11:00〜15:00 に、サーバーの移転作業のためメンテナンスを行います。\n\n■ メンテナンス後\n・みなさんのデータはそのまま引き継がれますが、再度ログインが必要となります。\n・これまでと同じ方法（メールアドレスとパスワード、または Google）でログインできます。\n\n■「ログイン無しでプレイ」で遊んでいる方へ\nメンテナンス後は、ログイン無しで遊んでいたデータに再びログインすることができません。\n続けて遊ぶ場合は、11/6(金) 11:00 までに「プレイヤー設定」の「アカウント連携」から、メールアドレスとパスワードの登録、または Google アカウントの連携を行ってください。",
                        new DateTimeOffset(2026, 11, 3, 15, 0, 4, TimeSpan.Zero)
                    },
                    {
                        "2026-08-12-balance-adjustment",
                        new DateOnly(2026, 8, 12),
                        "バランス調整を行いました",
                        "ゲームバランスの調整を行いました。\n\n■ 調整内容\n・各ステージの敵の能力値（HP・攻撃力・防御力など）を見直しました。\n・魔法使いのレベルアップ時の知力の伸びを見直しました。\n\n引き続き、遊びやすいバランスになるよう調整を続けていきます。",
                        new DateTimeOffset(2026, 8, 11, 15, 0, 3, TimeSpan.Zero)
                    },
                    {
                        "2026-08-11-progress-wipe",
                        new DateOnly(2026, 8, 5),
                        "【重要】8/11(火) プレイヤーデータのリセットについて",
                        "8/11(火) に、ゲームバランスの大幅な調整を行います。\nレベルの上限を100に、ジョブのレベルをプレイヤーのレベルに合わせて上がる方式へ変更します。加えて敵・装備等の数値も調整を行います。\nこの更新に合わせて みなさんのデータをリセットさせていただきます。\n\n■ 消えるもの\n・レベル / 経験値 / ジョブ / 転生の回数\n・ゴールド、持っているアイテム、装備\n・技、マスターしたジョブ\n・なかまのペット、ペット対戦の成績\n・宝の地図\n・マーケットの出品と取引の記録\n\n■ 残るもの\n・プレイヤー名\n・アイコン画像\n・チャットのログ\n・掲示板の書き込み\n\n■ リセットのあと\nレベル1・ゴールド100・技は「ルーキーストライク」・最初の装備を身につけた状態となります。",
                        new DateTimeOffset(2026, 8, 4, 15, 0, 2, TimeSpan.Zero)
                    },
                    {
                        "2026-08-02-announcement",
                        new DateOnly(2026, 8, 2),
                        "お知らせ機能を追加しました",
                        "チャット欄の「お知らせ」タブから、運営からのお知らせを確認できるようになりました。\n新しいお知らせがあるときは、タブに印が付きます。",
                        new DateTimeOffset(2026, 8, 1, 15, 0, 1, TimeSpan.Zero)
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "announcements",
                schema: "internal");
        }
    }
}
