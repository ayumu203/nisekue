using Microsoft.EntityFrameworkCore;
using server.domain.player;

namespace server.infrastructure.player;

internal sealed record PlayerAggregate(
    Player Player,
    PlayerMoveEntity? MoveEntity,
    IReadOnlyList<PlayerMasterJobEntity> MasteredJobEntities);

internal static class PlayerAggregatePersistence
{
    public static async Task<PlayerAggregate> LoadAsync(AppDbContext dbContext, PlayerEntity entity)
    {
        var moveEntity = await dbContext.PlayerMoves
            .SingleOrDefaultAsync(x => x.PlayerId == entity.Id);
        var masteredJobEntities = await dbContext.PlayerMasterJobs
            .Where(x => x.PlayerId == entity.Id)
            .ToListAsync();

        var player = PlayerEntityMapper.MapToDomain(entity, moveEntity, masteredJobEntities);
        return new PlayerAggregate(player, moveEntity, masteredJobEntities);
    }

    public static void Apply(AppDbContext dbContext, PlayerEntity entity, PlayerAggregate aggregate)
    {
        PlayerEntityMapper.ApplyPlayerEntity(entity, aggregate.Player);
        if (aggregate.MoveEntity is null)
        {
            dbContext.PlayerMoves.Add(PlayerEntityMapper.CreateMoveEntity(aggregate.Player.Id, aggregate.Player.MoveSet));
        }
        else
        {
            PlayerEntityMapper.ApplyMoveSet(aggregate.MoveEntity, aggregate.Player.MoveSet);
        }

        PlayerEntityMapper.SyncMasteredJobs(dbContext, aggregate.Player, aggregate.MasteredJobEntities);
    }
}
