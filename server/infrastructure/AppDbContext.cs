using Microsoft.EntityFrameworkCore;
using server.domain.chat;
using server.domain.player;
using server.shared.constants.chat;
using server.shared.constants.player;
using server.infrastructure.chat;
using server.infrastructure.player;

namespace server.infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();
    public DbSet<PlayerMoveEntity> PlayerMoves => Set<PlayerMoveEntity>();
    public DbSet<ChatRoomEntity> ChatRooms => Set<ChatRoomEntity>();
    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();

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
        chatMessage.Property(x => x.SenderId)
            .HasColumnName("sender_id")
            .HasColumnType("uuid")
            .HasConversion(x => x.Value, value => new PlayerId(value))
            .IsRequired();
        chatMessage.Property(x => x.Message)
            .HasColumnName("message")
            .HasMaxLength(ChatConstants.MessageMaxLength)
            .IsRequired();
        chatMessage.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
    }
}
