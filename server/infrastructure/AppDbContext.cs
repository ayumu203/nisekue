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
    }
}
