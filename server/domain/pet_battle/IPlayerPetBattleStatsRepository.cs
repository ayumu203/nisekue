using server.domain.player;

namespace server.domain.pet_battle;

public interface IPlayerPetBattleStatsRepository
{
    Task<PlayerPetBattleStats?> GetByPlayerIdAsync(PlayerId playerId);
    Task UpsertAsync(PlayerPetBattleStats stats);
}
