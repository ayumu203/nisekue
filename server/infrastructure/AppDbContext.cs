using Microsoft.EntityFrameworkCore;
using server.domain.chat;
using server.domain.player;
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
            .HasMaxLength(Player.NameMaxLength)
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
        chatRoom.HasKey(x => x.Owner_id);
        chatRoom.Property(x => x.Owner_id)
            .HasColumnName("owner_id")
            .HasColumnType("uuid")
            .HasConversion(x => x.Value, value => new PlayerId(value))
            .IsRequired();
        chatRoom.Property(x => x.Last_chat_id)
            .HasColumnName("last_chat_id")
            .IsRequired();

        var chatMessage = modelBuilder.Entity<ChatMessageEntity>();
        chatMessage.ToTable("chat_messages", "internal");
        chatMessage.HasKey(x => new { x.Owner_id, x.Chat_id });
        chatMessage.Property(x => x.Owner_id)
            .HasColumnName("owner_id")
            .HasColumnType("uuid")
            .HasConversion(x => x.Value, value => new PlayerId(value))
            .IsRequired();
        chatMessage.Property(x => x.Chat_id)
            .HasColumnName("chat_id")
            .IsRequired();
        chatMessage.Property(x => x.Sender_id)
            .HasColumnName("sender_id")
            .HasColumnType("uuid")
            .HasConversion(x => x.Value, value => new PlayerId(value))
            .IsRequired();
        chatMessage.Property(x => x.Message)
            .HasColumnName("message")
            .HasMaxLength(ChatText.MessageMaxLength)
            .IsRequired();
        chatMessage.Property(x => x.Created_at)
            .HasColumnName("created_at")
            .IsRequired();
    }
}
