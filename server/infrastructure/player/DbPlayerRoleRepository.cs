using Microsoft.EntityFrameworkCore;
using server.domain.player;

namespace server.infrastructure.player;

public class DbPlayerRoleRepository(
    IDbContextFactory<AppDbContext> dbContextFactory) : IPlayerRoleRepository
{
    public const string AdminValue = "admin";
    public const string PosterValue = "poster";

    public async Task<PlayerRole?> FindRoleAsync(PlayerId id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var role = await dbContext.Players
            .AsNoTracking()
            .Where(x => x.Id == id.Value)
            .Select(x => x.Role)
            .FirstOrDefaultAsync();

        return Parse(role);
    }

    public async Task<bool> SetRoleAsync(PlayerId id, PlayerRole? role)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var player = await dbContext.Players.SingleOrDefaultAsync(x => x.Id == id.Value);
        if (player is null)
        {
            return false;
        }

        player.Role = ToValue(role);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public static PlayerRole? Parse(string? value)
    {
        var normalized = value?.Trim();
        if (string.Equals(normalized, AdminValue, StringComparison.OrdinalIgnoreCase))
        {
            return PlayerRole.Admin;
        }

        if (string.Equals(normalized, PosterValue, StringComparison.OrdinalIgnoreCase))
        {
            return PlayerRole.Poster;
        }

        return null;
    }

    public static string? ToValue(PlayerRole? role) => role switch
    {
        PlayerRole.Admin => AdminValue,
        PlayerRole.Poster => PosterValue,
        _ => null
    };
}
