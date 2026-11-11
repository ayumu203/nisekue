using Microsoft.EntityFrameworkCore;
using server.domain.player;

namespace server.infrastructure.player;

public class DbPlayerRoleRepository(
    IDbContextFactory<AppDbContext> dbContextFactory) : IPlayerRoleRepository
{
    public const string AdminValue = "admin";

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

    private static PlayerRole? Parse(string? value)
    {
        return string.Equals(value?.Trim(), AdminValue, StringComparison.OrdinalIgnoreCase)
            ? PlayerRole.Admin
            : null;
    }
}
