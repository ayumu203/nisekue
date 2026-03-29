using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class AddItemAndMarketFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "gold",
                schema: "internal",
                table: "players",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "sender_id",
                schema: "internal",
                table: "chat_messages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "sender_type",
                schema: "internal",
                table: "chat_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "item_delete_logs",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_delete_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "market_listings",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_equipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_id = table.Column<int>(type: "integer", nullable: true),
                    item_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    flavor_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    remaining_quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<int>(type: "integer", nullable: false),
                    listed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_listings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "market_trade_histories",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<int>(type: "integer", nullable: false),
                    purchased_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_trade_histories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_item_stacks",
                schema: "internal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_item_stacks", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_item_stacks_players_player_id",
                        column: x => x.player_id,
                        principalSchema: "internal",
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_item_delete_logs_deleted_at",
                schema: "internal",
                table: "item_delete_logs",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_item_delete_logs_player_id",
                schema: "internal",
                table: "item_delete_logs",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_market_listings_expires_at",
                schema: "internal",
                table: "market_listings",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_market_listings_seller_id",
                schema: "internal",
                table: "market_listings",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_market_trade_histories_buyer_id",
                schema: "internal",
                table: "market_trade_histories",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "IX_market_trade_histories_seller_id",
                schema: "internal",
                table: "market_trade_histories",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_item_stacks_player_id_item_id",
                schema: "internal",
                table: "player_item_stacks",
                columns: new[] { "player_id", "item_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_delete_logs",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "market_listings",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "market_trade_histories",
                schema: "internal");

            migrationBuilder.DropTable(
                name: "player_item_stacks",
                schema: "internal");

            migrationBuilder.DropColumn(
                name: "gold",
                schema: "internal",
                table: "players");

            migrationBuilder.DropColumn(
                name: "sender_type",
                schema: "internal",
                table: "chat_messages");

            migrationBuilder.AlterColumn<Guid>(
                name: "sender_id",
                schema: "internal",
                table: "chat_messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
