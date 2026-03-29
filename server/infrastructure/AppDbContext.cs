using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using server.domain.chat;
using server.domain.player;
using server.shared.constants.chat;
using server.shared.constants.player;
using server.infrastructure.chat;
using server.infrastructure.player;
using server.infrastructure.quest.room;
using server.infrastructure.quest.run;

namespace server.infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();
    public DbSet<PlayerMoveEntity> PlayerMoves => Set<PlayerMoveEntity>();
    public DbSet<PlayerMasterJobEntity> PlayerMasterJobs => Set<PlayerMasterJobEntity>();
    public DbSet<PlayerEquipmentEntity> PlayerEquipments => Set<PlayerEquipmentEntity>();
    public DbSet<PlayerItemStackEntity> PlayerItemStacks => Set<PlayerItemStackEntity>();
    public DbSet<MarketListingEntity> MarketListings => Set<MarketListingEntity>();
    public DbSet<MarketTradeHistoryEntity> MarketTradeHistories => Set<MarketTradeHistoryEntity>();
    public DbSet<ItemDeletionLogEntity> ItemDeletionLogs => Set<ItemDeletionLogEntity>();
    public DbSet<ChatRoomEntity> ChatRooms => Set<ChatRoomEntity>();
    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();
    public DbSet<ThreadEntity> Threads => Set<ThreadEntity>();
    public DbSet<ThreadReplyEntity> ThreadReplies => Set<ThreadReplyEntity>();
    public DbSet<QuestRoomEntity> QuestRooms => Set<QuestRoomEntity>();
    public DbSet<QuestRoomAllowedPlayerEntity> QuestRoomAllowedPlayers => Set<QuestRoomAllowedPlayerEntity>();
    public DbSet<QuestRoomParticipantEntity> QuestRoomParticipants => Set<QuestRoomParticipantEntity>();
    public DbSet<QuestRunEntity> QuestRuns => Set<QuestRunEntity>();
    public DbSet<QuestRunPartySnapshotEntity> QuestRunPartySnapshots => Set<QuestRunPartySnapshotEntity>();
    public DbSet<QuestRunPartyMemberEntity> QuestRunPartyMembers => Set<QuestRunPartyMemberEntity>();
    public DbSet<QuestRunEnemyEntity> QuestRunEnemies => Set<QuestRunEnemyEntity>();
    public DbSet<QuestTurnCommandEntity> QuestTurnCommands => Set<QuestTurnCommandEntity>();
    public DbSet<QuestFloorTrapEntity> QuestFloorTraps => Set<QuestFloorTrapEntity>();
    public DbSet<QuestRewardSummaryEntity> QuestRewardSummaries => Set<QuestRewardSummaryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var player = modelBuilder.Entity<PlayerEntity>();
        player.ToTable("players", "internal");
        player.HasKey(x => x.Id);
        player.Property(x => x.Id).HasColumnName("id");
        player.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(PlayerConstants.NameMaxLength)
            .IsRequired();
        player.Property(x => x.ImagePath)
            .HasColumnName("image_path")
            .HasMaxLength(PlayerConstants.ImagePathMaxLength);
        player.Property(x => x.Job)
            .HasColumnName("job")
            .HasConversion<int>()
            .HasDefaultValue(Job.Apprentice)
            .IsRequired();
        player.Property(x => x.Level)
            .HasColumnName("level")
            .IsRequired();
        player.Property(x => x.Exp)
            .HasColumnName("exp")
            .IsRequired();
        player.Property(x => x.JobLevel)
            .HasColumnName("job_level")
            .IsRequired();
        player.Property(x => x.JobExp)
            .HasColumnName("job_exp")
            .IsRequired();
        player.Property(x => x.Gold)
            .HasColumnName("gold")
            .HasDefaultValue(100)
            .IsRequired();
        player.Property(x => x.MaxHp)
            .HasColumnName("max_hp")
            .IsRequired();
        player.Property(x => x.MaxMp)
            .HasColumnName("max_mp")
            .IsRequired();
        player.Property(x => x.Strength)
            .HasColumnName("strength")
            .IsRequired();
        player.Property(x => x.Defense)
            .HasColumnName("defense")
            .IsRequired();
        player.Property(x => x.Intelligence)
            .HasColumnName("intelligence")
            .IsRequired();
        player.Property(x => x.Luck)
            .HasColumnName("luck")
            .IsRequired();
        player.Property(x => x.Speed)
            .HasColumnName("speed")
            .IsRequired();
        player.Property(x => x.TrainingBattleCount)
            .HasColumnName("training_battle_count")
            .HasDefaultValue(0)
            .IsRequired();
        player.Property(x => x.TrainingCooldownUntil)
            .HasColumnName("training_cooldown_until");
        player.Property(x => x.QuestCooldownUntil)
            .HasColumnName("quest_cooldown_until");

        var playerMoves = modelBuilder.Entity<PlayerMoveEntity>();
        playerMoves.ToTable("player_moves", "internal");
        playerMoves.HasKey(x => x.PlayerId);
        playerMoves.Property(x => x.PlayerId)
            .HasColumnName("player_id")
            .HasColumnType("uuid")
            .IsRequired();
        playerMoves.Property(x => x.MoveId1).HasColumnName("move_id_1");
        playerMoves.Property(x => x.MoveId2).HasColumnName("move_id_2");
        playerMoves.Property(x => x.MoveId3).HasColumnName("move_id_3");
        playerMoves.Property(x => x.MoveId4).HasColumnName("move_id_4");
        playerMoves.Property(x => x.MoveId5).HasColumnName("move_id_5");
        playerMoves.Property(x => x.MoveId6).HasColumnName("move_id_6");
        playerMoves.Property(x => x.MoveId7).HasColumnName("move_id_7");
        playerMoves.Property(x => x.MoveId8).HasColumnName("move_id_8");
        playerMoves.Property(x => x.MoveId9).HasColumnName("move_id_9");
        playerMoves.Property(x => x.MoveId10).HasColumnName("move_id_10");
        playerMoves
            .HasOne<PlayerEntity>()
            .WithOne()
            .HasForeignKey<PlayerMoveEntity>(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        var playerMasterJobs = modelBuilder.Entity<PlayerMasterJobEntity>();
        playerMasterJobs.ToTable("player_master_jobs", "internal");
        playerMasterJobs.HasKey(x => new { x.PlayerId, x.Job });
        playerMasterJobs.Property(x => x.PlayerId)
            .HasColumnName("player_id")
            .HasColumnType("uuid")
            .IsRequired();
        playerMasterJobs.Property(x => x.Job)
            .HasColumnName("job")
            .HasConversion<int>()
            .IsRequired();
        playerMasterJobs.Property(x => x.MasteredAt)
            .HasColumnName("mastered_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        playerMasterJobs
            .HasOne<PlayerEntity>()
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        var playerEquipment = modelBuilder.Entity<PlayerEquipmentEntity>();
        playerEquipment.ToTable("player_equipments", "internal");
        playerEquipment.HasKey(x => x.Id);
        playerEquipment.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").IsRequired();
        playerEquipment.Property(x => x.PlayerId).HasColumnName("player_id").HasColumnType("uuid").IsRequired();
        playerEquipment.Property(x => x.EquipmentId).HasColumnName("equipment_id").IsRequired();
        playerEquipment.Property(x => x.EquipmentType).HasColumnName("equipment_type").IsRequired();
        playerEquipment.Property(x => x.EquipmentStatus).HasColumnName("equipment_status").IsRequired();
        playerEquipment.Property(x => x.Durability).HasColumnName("durability").IsRequired();
        playerEquipment.Property(x => x.Mastery).HasColumnName("mastery").HasDefaultValue(0).IsRequired();
        playerEquipment.Property(x => x.AcquiredAt).HasColumnName("acquired_at").IsRequired();
        playerEquipment.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        playerEquipment.HasIndex(x => x.PlayerId);
        playerEquipment
            .HasOne<PlayerEntity>()
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        var playerItemStack = modelBuilder.Entity<PlayerItemStackEntity>();
        playerItemStack.ToTable("player_item_stacks", "internal");
        playerItemStack.HasKey(x => x.Id);
        playerItemStack.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").IsRequired();
        playerItemStack.Property(x => x.PlayerId).HasColumnName("player_id").HasColumnType("uuid").IsRequired();
        playerItemStack.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
        playerItemStack.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        playerItemStack.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        playerItemStack.HasIndex(x => new { x.PlayerId, x.ItemId }).IsUnique();
        playerItemStack
            .HasOne<PlayerEntity>()
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        var marketListing = modelBuilder.Entity<MarketListingEntity>();
        marketListing.ToTable("market_listings", "internal");
        marketListing.HasKey(x => x.Id);
        marketListing.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").IsRequired();
        marketListing.Property(x => x.SellerId).HasColumnName("seller_id").HasColumnType("uuid").IsRequired();
        marketListing.Property(x => x.PlayerEquipmentId).HasColumnName("player_equipment_id").HasColumnType("uuid");
        marketListing.Property(x => x.ItemId).HasColumnName("item_id");
        marketListing.Property(x => x.ItemName).HasColumnName("item_name").HasMaxLength(100).IsRequired();
        marketListing.Property(x => x.FlavorText).HasColumnName("flavor_text").HasMaxLength(500).IsRequired();
        marketListing.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        marketListing.Property(x => x.RemainingQuantity).HasColumnName("remaining_quantity").IsRequired();
        marketListing.Property(x => x.UnitPrice).HasColumnName("unit_price").IsRequired();
        marketListing.Property(x => x.ListedAt).HasColumnName("listed_at").IsRequired();
        marketListing.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
        marketListing.HasIndex(x => x.SellerId);
        marketListing.HasIndex(x => x.ExpiresAt);

        var marketTradeHistory = modelBuilder.Entity<MarketTradeHistoryEntity>();
        marketTradeHistory.ToTable("market_trade_histories", "internal");
        marketTradeHistory.HasKey(x => x.Id);
        marketTradeHistory.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").IsRequired();
        marketTradeHistory.Property(x => x.SellerId).HasColumnName("seller_id").HasColumnType("uuid").IsRequired();
        marketTradeHistory.Property(x => x.BuyerId).HasColumnName("buyer_id").HasColumnType("uuid").IsRequired();
        marketTradeHistory.Property(x => x.ItemIdentifier).HasColumnName("item_id").HasMaxLength(50).IsRequired();
        marketTradeHistory.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        marketTradeHistory.Property(x => x.UnitPrice).HasColumnName("unit_price").IsRequired();
        marketTradeHistory.Property(x => x.PurchasedAt).HasColumnName("purchased_at").IsRequired();
        marketTradeHistory.HasIndex(x => x.SellerId);
        marketTradeHistory.HasIndex(x => x.BuyerId);

        var itemDeletionLog = modelBuilder.Entity<ItemDeletionLogEntity>();
        itemDeletionLog.ToTable("item_delete_logs", "internal");
        itemDeletionLog.HasKey(x => x.Id);
        itemDeletionLog.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").IsRequired();
        itemDeletionLog.Property(x => x.PlayerId).HasColumnName("player_id").HasColumnType("uuid").IsRequired();
        itemDeletionLog.Property(x => x.ItemIdentifier).HasColumnName("item_id").HasMaxLength(50).IsRequired();
        itemDeletionLog.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
        itemDeletionLog.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(50).IsRequired();
        itemDeletionLog.Property(x => x.DeletedAt).HasColumnName("deleted_at").IsRequired();
        itemDeletionLog.HasIndex(x => x.PlayerId);
        itemDeletionLog.HasIndex(x => x.DeletedAt);

        var chatRoom = modelBuilder.Entity<ChatRoomEntity>();
        chatRoom.ToTable("chat_rooms", "internal");
        chatRoom.HasKey(x => x.OwnerId);
        chatRoom.Property(x => x.OwnerId)
            .HasColumnName("owner_id")
            .HasColumnType("uuid")
            .HasConversion(x => x.Value, value => new PlayerId(value))
            .IsRequired();
        chatRoom.Property(x => x.LastChatId)
            .HasColumnName("last_chat_id")
            .IsRequired();

        var chatMessage = modelBuilder.Entity<ChatMessageEntity>();
        chatMessage.ToTable("chat_messages", "internal");
        chatMessage.HasKey(x => new { x.OwnerId, x.ChatId });
        chatMessage.Property(x => x.OwnerId)
            .HasColumnName("owner_id")
            .HasColumnType("uuid")
            .HasConversion(x => x.Value, value => new PlayerId(value))
            .IsRequired();
        chatMessage.Property(x => x.ChatId)
            .HasColumnName("chat_id")
            .IsRequired();
        chatMessage.Property(x => x.SenderType)
            .HasColumnName("sender_type")
            .HasConversion<int>()
            .IsRequired();
        chatMessage.Property(x => x.SenderId)
            .HasColumnName("sender_id")
            .HasColumnType("uuid")
            .HasConversion(new ValueConverter<PlayerId?, Guid?>(
                x => x == null ? null : x.Value.Value,
                value => value == null ? null : new PlayerId(value.Value)));
        chatMessage.Property(x => x.Message)
            .HasColumnName("message")
            .HasMaxLength(ChatConstants.MessageMaxLength)
            .IsRequired();
        chatMessage.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        var thread = modelBuilder.Entity<ThreadEntity>();
        thread.ToTable("threads", "internal");
        thread.HasKey(x => x.Id);
        thread.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(x => x.Value, value => new ThreadId(value))
            .IsRequired();
        thread.Property(x => x.AuthorPlayerId)
            .HasColumnName("author_player_id")
            .HasColumnType("uuid")
            .IsRequired();
        thread.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(ThreadConstants.TitleMaxLength)
            .IsRequired();
        thread.Property(x => x.Body)
            .HasColumnName("body")
            .HasMaxLength(ThreadConstants.BodyMaxLength)
            .IsRequired();
        thread.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        thread.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
        thread.Property(x => x.LastRepliedAt)
            .HasColumnName("last_replied_at");
        thread.HasIndex(x => x.AuthorPlayerId);
        thread.HasIndex(x => x.CreatedAt);
        thread
            .HasOne<PlayerEntity>()
            .WithMany()
            .HasForeignKey(x => x.AuthorPlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        var threadReply = modelBuilder.Entity<ThreadReplyEntity>();
        threadReply.ToTable("thread_replies", "internal");
        threadReply.HasKey(x => x.Id);
        threadReply.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasConversion(x => x.Value, value => new ThreadReplyId(value))
            .IsRequired();
        threadReply.Property(x => x.ThreadId)
            .HasColumnName("thread_id")
            .HasColumnType("uuid")
            .HasConversion(x => x.Value, value => new ThreadId(value))
            .IsRequired();
        threadReply.Property(x => x.AuthorPlayerId)
            .HasColumnName("author_player_id")
            .HasColumnType("uuid")
            .IsRequired();
        threadReply.Property(x => x.Body)
            .HasColumnName("body")
            .HasMaxLength(ThreadConstants.ReplyBodyMaxLength)
            .IsRequired();
        threadReply.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        threadReply.HasIndex(x => x.ThreadId);
        threadReply.HasIndex(x => x.AuthorPlayerId);
        threadReply
            .HasOne<PlayerEntity>()
            .WithMany()
            .HasForeignKey(x => x.AuthorPlayerId)
            .OnDelete(DeleteBehavior.Cascade);
        threadReply
            .HasOne<ThreadEntity>()
            .WithMany()
            .HasForeignKey(x => x.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        var questRoom = modelBuilder.Entity<QuestRoomEntity>();
        questRoom.ToTable("quest_rooms", "internal");
        questRoom.HasKey(x => x.Id);
        questRoom.Property(x => x.Id).HasColumnName("id");
        questRoom.Property(x => x.OwnerPlayerId).HasColumnName("owner_player_id").HasColumnType("uuid").IsRequired();
        questRoom.Property(x => x.StageId).HasColumnName("stage_id").IsRequired();
        questRoom.Property(x => x.Mode).HasColumnName("mode").IsRequired();
        questRoom.Property(x => x.Status).HasColumnName("status").IsRequired();
        questRoom.Property(x => x.MinRequiredLevel).HasColumnName("min_required_level");
        questRoom.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken().IsRequired();
        questRoom.Property(x => x.CloseReason).HasColumnName("close_reason");
        questRoom.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        questRoom.Property(x => x.ClosedAt).HasColumnName("closed_at");
        questRoom.HasIndex(x => x.OwnerPlayerId)
            .IsUnique()
            .HasFilter($"status = {(int)server.domain.quest.enums.QuestRoomStatus.Recruiting}");

        var questRoomAllowedPlayer = modelBuilder.Entity<QuestRoomAllowedPlayerEntity>();
        questRoomAllowedPlayer.ToTable("quest_room_allowed_players", "internal");
        questRoomAllowedPlayer.HasKey(x => new { x.RoomId, x.PlayerId });
        questRoomAllowedPlayer.Property(x => x.RoomId).HasColumnName("room_id").HasColumnType("uuid").IsRequired();
        questRoomAllowedPlayer.Property(x => x.PlayerId).HasColumnName("player_id").HasColumnType("uuid").IsRequired();
        questRoomAllowedPlayer.Property(x => x.AddedAt).HasColumnName("added_at").IsRequired();
        questRoomAllowedPlayer.HasOne<QuestRoomEntity>()
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
        questRoomAllowedPlayer.HasOne<PlayerEntity>()
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        var questRoomParticipant = modelBuilder.Entity<QuestRoomParticipantEntity>();
        questRoomParticipant.ToTable("quest_room_participants", "internal");
        questRoomParticipant.HasKey(x => x.Id);
        questRoomParticipant.Property(x => x.Id).HasColumnName("id");
        questRoomParticipant.Property(x => x.RoomId).HasColumnName("room_id").HasColumnType("uuid").IsRequired();
        questRoomParticipant.Property(x => x.ParticipantType).HasColumnName("participant_type").IsRequired();
        questRoomParticipant.Property(x => x.PlayerId).HasColumnName("player_id").HasColumnType("uuid");
        questRoomParticipant.Property(x => x.NpcTemplateId).HasColumnName("npc_template_id");
        questRoomParticipant.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
        questRoomParticipant.Property(x => x.BattleRow).HasColumnName("battle_row").IsRequired();
        questRoomParticipant.Property(x => x.BattleColumn).HasColumnName("battle_column").IsRequired();
        questRoomParticipant.Property(x => x.ParticipantStatus).HasColumnName("participant_status").IsRequired();
        questRoomParticipant.Property(x => x.IsOwner).HasColumnName("is_owner").IsRequired();
        questRoomParticipant.Property(x => x.JoinedAt).HasColumnName("joined_at").IsRequired();
        questRoomParticipant.Property(x => x.LastSeenAt).HasColumnName("last_seen_at");
        questRoomParticipant.Property(x => x.LeftAt).HasColumnName("left_at");
        questRoomParticipant.HasIndex(x => new { x.RoomId, x.BattleRow, x.BattleColumn }).IsUnique();

        var questRun = modelBuilder.Entity<QuestRunEntity>();
        questRun.ToTable("quest_runs", "internal");
        questRun.HasKey(x => x.Id);
        questRun.Property(x => x.Id).HasColumnName("id");
        questRun.Property(x => x.RoomId).HasColumnName("room_id").HasColumnType("uuid").IsRequired();
        questRun.HasIndex(x => x.RoomId).IsUnique();
        questRun.Property(x => x.StageId).HasColumnName("stage_id").IsRequired();
        questRun.Property(x => x.Status).HasColumnName("status").IsRequired();
        questRun.Property(x => x.CurrentFloorNo).HasColumnName("current_floor_no").IsRequired();
        questRun.Property(x => x.CurrentTurnNo).HasColumnName("current_turn_no").IsRequired();
        questRun.Property(x => x.ActionDeadlineAt).HasColumnName("action_deadline_at").IsRequired();
        questRun.Property(x => x.LastResolvedTurnNo).HasColumnName("last_resolved_turn_no");
        questRun.Property(x => x.LastTurnResultsJson).HasColumnName("last_turn_results_json").HasColumnType("jsonb");
        questRun.Property(x => x.ChatMessagesJson).HasColumnName("chat_messages_json").HasColumnType("jsonb").IsRequired();
        questRun.Property(x => x.StartedAt).HasColumnName("started_at").IsRequired();
        questRun.Property(x => x.EndedAt).HasColumnName("ended_at");

        var questRunPartySnapshot = modelBuilder.Entity<QuestRunPartySnapshotEntity>();
        questRunPartySnapshot.ToTable("quest_run_party_snapshots", "internal");
        questRunPartySnapshot.HasKey(x => new { x.RunId, x.ParticipantId });
        questRunPartySnapshot.Property(x => x.RunId).HasColumnName("run_id").HasColumnType("uuid").IsRequired();
        questRunPartySnapshot.Property(x => x.ParticipantId).HasColumnName("participant_id").HasColumnType("uuid").IsRequired();
        questRunPartySnapshot.Property(x => x.ParticipantType).HasColumnName("participant_type").IsRequired();
        questRunPartySnapshot.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
        questRunPartySnapshot.Property(x => x.ImagePath).HasColumnName("image_path").HasMaxLength(255);
        questRunPartySnapshot.Property(x => x.Job).HasColumnName("job").IsRequired();
        questRunPartySnapshot.Property(x => x.WeaponPlayerEquipmentId).HasColumnName("weapon_player_equipment_id").HasColumnType("uuid");
        questRunPartySnapshot.Property(x => x.ArmorPlayerEquipmentId).HasColumnName("armor_player_equipment_id").HasColumnType("uuid");
        questRunPartySnapshot.Property(x => x.StartRow).HasColumnName("start_row").IsRequired();
        questRunPartySnapshot.Property(x => x.StartColumn).HasColumnName("start_column").IsRequired();
        questRunPartySnapshot.Property(x => x.MaxHp).HasColumnName("max_hp").IsRequired();
        questRunPartySnapshot.Property(x => x.MaxMp).HasColumnName("max_mp").IsRequired();
        questRunPartySnapshot.Property(x => x.Strength).HasColumnName("strength").IsRequired();
        questRunPartySnapshot.Property(x => x.Defense).HasColumnName("defense").IsRequired();
        questRunPartySnapshot.Property(x => x.Intelligence).HasColumnName("intelligence").IsRequired();
        questRunPartySnapshot.Property(x => x.Luck).HasColumnName("luck").IsRequired();
        questRunPartySnapshot.Property(x => x.Speed).HasColumnName("speed").IsRequired();
        questRunPartySnapshot.Property(x => x.MoveSetJson).HasColumnName("move_set_json").HasColumnType("jsonb").IsRequired();
        questRunPartySnapshot.Property(x => x.InitialActionMode).HasColumnName("initial_action_mode").IsRequired();

        var questRunPartyMember = modelBuilder.Entity<QuestRunPartyMemberEntity>();
        questRunPartyMember.ToTable("quest_run_party_members", "internal");
        questRunPartyMember.HasKey(x => new { x.RunId, x.ParticipantId });
        questRunPartyMember.Property(x => x.RunId).HasColumnName("run_id").HasColumnType("uuid").IsRequired();
        questRunPartyMember.Property(x => x.ParticipantId).HasColumnName("participant_id").HasColumnType("uuid").IsRequired();
        questRunPartyMember.Property(x => x.CurrentHp).HasColumnName("current_hp").IsRequired();
        questRunPartyMember.Property(x => x.CurrentMp).HasColumnName("current_mp").IsRequired();
        questRunPartyMember.Property(x => x.IsDead).HasColumnName("is_dead").IsRequired();
        questRunPartyMember.Property(x => x.CanActFromTurn).HasColumnName("can_act_from_turn").IsRequired();
        questRunPartyMember.Property(x => x.ActionMode).HasColumnName("action_mode").IsRequired();
        questRunPartyMember.Property(x => x.HasLeftQuest).HasColumnName("has_left_quest").IsRequired();
        questRunPartyMember.Property(x => x.IsManualControlRequested).HasColumnName("is_manual_control_requested").IsRequired();
        questRunPartyMember.Property(x => x.ActiveEffectsJson).HasColumnName("active_effects_json").HasColumnType("jsonb").IsRequired();
        questRunPartyMember.Property(x => x.DerivedParametersJson).HasColumnName("derived_parameters_json").HasColumnType("jsonb").IsRequired();
        questRunPartyMember.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

        var questRunEnemy = modelBuilder.Entity<QuestRunEnemyEntity>();
        questRunEnemy.ToTable("quest_run_enemies", "internal");
        questRunEnemy.HasKey(x => new { x.RunId, x.EnemyInstanceId });
        questRunEnemy.Property(x => x.RunId).HasColumnName("run_id").HasColumnType("uuid").IsRequired();
        questRunEnemy.Property(x => x.EnemyInstanceId).HasColumnName("enemy_instance_id").HasColumnType("uuid").IsRequired();
        questRunEnemy.Property(x => x.FloorNo).HasColumnName("floor_no").IsRequired();
        questRunEnemy.Property(x => x.EnemyDefinitionId).HasColumnName("enemy_definition_id").IsRequired();
        questRunEnemy.Property(x => x.BattleRow).HasColumnName("battle_row").IsRequired();
        questRunEnemy.Property(x => x.BattleColumn).HasColumnName("battle_column").IsRequired();
        questRunEnemy.Property(x => x.CurrentHp).HasColumnName("current_hp").IsRequired();
        questRunEnemy.Property(x => x.CurrentMp).HasColumnName("current_mp").IsRequired();
        questRunEnemy.Property(x => x.IsDead).HasColumnName("is_dead").IsRequired();
        questRunEnemy.Property(x => x.ActiveEffectsJson).HasColumnName("active_effects_json").HasColumnType("jsonb").IsRequired();
        questRunEnemy.Property(x => x.DerivedParametersJson).HasColumnName("derived_parameters_json").HasColumnType("jsonb").IsRequired();

        var questTurnCommand = modelBuilder.Entity<QuestTurnCommandEntity>();
        questTurnCommand.ToTable("quest_turn_commands", "internal");
        questTurnCommand.HasKey(x => new { x.RunId, x.TurnNo, x.ParticipantId });
        questTurnCommand.Property(x => x.RunId).HasColumnName("run_id").HasColumnType("uuid").IsRequired();
        questTurnCommand.Property(x => x.TurnNo).HasColumnName("turn_no").IsRequired();
        questTurnCommand.Property(x => x.ParticipantId).HasColumnName("participant_id").HasColumnType("uuid").IsRequired();
        questTurnCommand.Property(x => x.ActionKind).HasColumnName("action_kind").IsRequired();
        questTurnCommand.Property(x => x.MoveId).HasColumnName("move_id");
        questTurnCommand.Property(x => x.TargetRow).HasColumnName("target_row");
        questTurnCommand.Property(x => x.TargetColumn).HasColumnName("target_column");
        questTurnCommand.Property(x => x.SubmittedAt).HasColumnName("submitted_at").IsRequired();
        questTurnCommand.Property(x => x.IsAutoSubmitted).HasColumnName("is_auto_submitted").IsRequired();

        var questFloorTrap = modelBuilder.Entity<QuestFloorTrapEntity>();
        questFloorTrap.ToTable("quest_floor_traps", "internal");
        questFloorTrap.HasKey(x => new { x.RunId, x.TrapId });
        questFloorTrap.Property(x => x.RunId).HasColumnName("run_id").HasColumnType("uuid").IsRequired();
        questFloorTrap.Property(x => x.TrapId).HasColumnName("trap_id").HasColumnType("uuid").IsRequired();
        questFloorTrap.Property(x => x.SourceParticipantId).HasColumnName("source_participant_id").HasColumnType("uuid").IsRequired();
        questFloorTrap.Property(x => x.MoveId).HasColumnName("move_id").IsRequired();
        questFloorTrap.Property(x => x.ExpiresAfterFloorNo).HasColumnName("expires_after_floor_no").IsRequired();
        questFloorTrap.Property(x => x.IsTriggered).HasColumnName("is_triggered").IsRequired();

        var questRewardSummary = modelBuilder.Entity<QuestRewardSummaryEntity>();
        questRewardSummary.ToTable("quest_reward_summaries", "internal");
        questRewardSummary.HasKey(x => x.RunId);
        questRewardSummary.Property(x => x.RunId).HasColumnName("run_id").HasColumnType("uuid").IsRequired();
        questRewardSummary.Property(x => x.Exp).HasColumnName("exp").IsRequired();
        questRewardSummary.Property(x => x.EquipmentRewardId).HasColumnName("equipment_reward_id");
        questRewardSummary.Property(x => x.SkippedRewardPlayerIdsJson).HasColumnName("skipped_reward_player_ids_json").HasColumnType("jsonb").IsRequired();
    }
}
