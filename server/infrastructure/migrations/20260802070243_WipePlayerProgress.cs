using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.infrastructure.migrations
{
    /// <inheritdoc />
    public partial class WipePlayerProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // バランス調整（レベル上限100・職業レベルのプレイヤーレベル連動）に伴う進行データの初期化。
            // 保持するのは プレイヤーの名前・画像・チャット・掲示板 のみで、他の進行データはすべて初期状態へ戻す。
            // この操作は不可逆である。

            // 1. 進行データの削除。FK があるものは子テーブルから先に消す。
            migrationBuilder.Sql("DELETE FROM internal.pet_battle_turn_commands;");
            migrationBuilder.Sql("DELETE FROM internal.pet_battle_runs;");
            migrationBuilder.Sql("DELETE FROM internal.pet_battle_rooms;");
            migrationBuilder.Sql("DELETE FROM internal.player_pet_battle_stats;");
            migrationBuilder.Sql("DELETE FROM internal.player_pets;");

            migrationBuilder.Sql("DELETE FROM internal.quest_turn_commands;");
            migrationBuilder.Sql("DELETE FROM internal.quest_run_enemies;");
            migrationBuilder.Sql("DELETE FROM internal.quest_run_party_members;");
            migrationBuilder.Sql("DELETE FROM internal.quest_run_party_snapshots;");
            migrationBuilder.Sql("DELETE FROM internal.quest_floor_traps;");
            migrationBuilder.Sql("DELETE FROM internal.quest_reward_summaries;");
            migrationBuilder.Sql("DELETE FROM internal.quest_runs;");
            migrationBuilder.Sql("DELETE FROM internal.quest_room_allowed_players;");
            migrationBuilder.Sql("DELETE FROM internal.quest_room_participants;");
            migrationBuilder.Sql("DELETE FROM internal.quest_rooms;");

            migrationBuilder.Sql("DELETE FROM internal.treasure_map_claim_histories;");
            migrationBuilder.Sql("DELETE FROM internal.treasure_map_expeditions;");

            migrationBuilder.Sql("DELETE FROM internal.ranking_entries;");
            migrationBuilder.Sql("DELETE FROM internal.ranking_snapshots;");

            migrationBuilder.Sql("DELETE FROM internal.market_listings;");
            migrationBuilder.Sql("DELETE FROM internal.market_trade_histories;");
            migrationBuilder.Sql("DELETE FROM internal.item_delete_logs;");

            migrationBuilder.Sql("DELETE FROM internal.player_rebirth_status_histories;");
            migrationBuilder.Sql("DELETE FROM internal.player_master_jobs;");
            migrationBuilder.Sql("DELETE FROM internal.player_item_stacks;");
            migrationBuilder.Sql("DELETE FROM internal.player_equipments;");
            migrationBuilder.Sql("DELETE FROM internal.player_moves;");

            // 2. プレイヤー本体を新規作成時の値に戻す。name / image_path / id は保持する。
            migrationBuilder.Sql(@"
                UPDATE internal.players SET
                    job = 1,
                    rebirth_count = 0,
                    level = 1,
                    exp = 0,
                    job_level = 1,
                    job_exp = 0,
                    gold = 100,
                    max_hp = 24,
                    max_mp = 8,
                    strength = 7,
                    defense = 5,
                    intelligence = 5,
                    luck = 3,
                    speed = 4,
                    training_battle_count = 0,
                    training_cooldown_until = NULL,
                    quest_cooldown_until = NULL,
                    pet_battle_cooldown_until = NULL,
                    exp_multiplier_flags = 0,
                    map_unlock_flags = 0,
                    roadmap_unlock_flags = 1,
                    endless_best_floor = 0;
            ");

            // 3. 初期技（ルーキーストライク）を再付与する。
            migrationBuilder.Sql(@"
                INSERT INTO internal.player_moves (player_id, move_id_1)
                SELECT id, 101 FROM internal.players;
            ");

            // 4. 初期装備を装備済み状態で再付与する。
            //    equipment_type: Weapon=1 / Armor=2、equipment_status: Equipped=2。
            //    durability は equipment_master.csv の max_durability（1001・2001 ともに 10）に合わせる。
            migrationBuilder.Sql(@"
                INSERT INTO internal.player_equipments
                    (id, player_id, equipment_id, equipment_type, equipment_status, durability, mastery, plus_value, acquired_at, updated_at)
                SELECT gen_random_uuid(), id, 1001, 1, 2, 10, 0, 0, now(), now() FROM internal.players
                UNION ALL
                SELECT gen_random_uuid(), id, 2001, 2, 2, 10, 0, 0, now(), now() FROM internal.players;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 削除した進行データは復元できないため、ロールバック時は何もしない。
        }
    }
}
