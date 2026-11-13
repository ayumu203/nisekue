using Microsoft.EntityFrameworkCore;
using server.domain.announcement;

namespace server.infrastructure.announcement;

public sealed class DbAnnouncementRepository(
    IDbContextFactory<AppDbContext> dbContextFactory) : IAnnouncementRepository
{
    public async Task<IReadOnlyList<Announcement>> ListAsync()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entities = await dbContext.Announcements
            .AsNoTracking()
            .ToListAsync();

        // SQLite（テスト）では DateTimeOffset の ORDER BY ができないため、件数の少ないお知らせはメモリ上で並べる
        return entities
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id, StringComparer.Ordinal)
            .Select(ToDomain)
            .ToList();
    }

    public async Task<bool> TryAddAsync(Announcement announcement)
    {
        ArgumentNullException.ThrowIfNull(announcement);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        if (await dbContext.Announcements.AnyAsync(x => x.Id == announcement.Id.Value))
        {
            return false;
        }

        dbContext.Announcements.Add(new AnnouncementEntity
        {
            Id = announcement.Id.Value,
            Date = announcement.Date,
            Title = announcement.Title,
            Body = announcement.Body,
            CreatedAt = announcement.CreatedAt
        });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // 同時に同じ ID が登録された場合
            if (await dbContext.Announcements.AsNoTracking().AnyAsync(x => x.Id == announcement.Id.Value))
            {
                return false;
            }

            throw;
        }

        return true;
    }

    public async Task<bool> DeleteAsync(AnnouncementId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var entity = await dbContext.Announcements.FirstOrDefaultAsync(x => x.Id == id.Value);
        if (entity is null)
        {
            return false;
        }

        dbContext.Announcements.Remove(entity);
        await dbContext.SaveChangesAsync();
        return true;
    }

    private static Announcement ToDomain(AnnouncementEntity entity)
    {
        return Announcement.Create(new AnnouncementId(entity.Id), entity.Date, entity.Title, entity.Body, entity.CreatedAt);
    }
}
