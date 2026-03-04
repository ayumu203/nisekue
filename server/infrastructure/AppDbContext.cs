using Microsoft.EntityFrameworkCore;
using server.domain.player;
using server.infrastructure.player;

namespace server.infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();

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
    }
}
