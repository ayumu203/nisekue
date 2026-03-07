using Microsoft.EntityFrameworkCore;
using server.domain.chat;
using server.domain.player;
using server.domain.shared;
using server.infrastructure.chat;
using server.infrastructure.player;

namespace server.infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();
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
            .HasMaxLength(DomainConstraints.Names.PlayerMaxLength)
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
            .HasMaxLength(ChatText.MessageMaxLength)
            .IsRequired();
        chatMessage.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
    }
}
