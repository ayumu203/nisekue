using server.domain.player;

namespace server.domain.pet_battle;

public interface IPetBattleRunRepository
{
    Task<PetBattleRun?> GetAsync(PetBattleRunId id);
    Task<PetBattleRun?> GetByRoomIdAsync(PetBattleRoomId roomId);
    Task<PetBattleRun?> GetActiveByOwnerAsync(PlayerId ownerId);
    Task<IReadOnlyList<PetBattleRun>> ListExpiredAsync(DateTimeOffset now);
    Task SaveAsync(PetBattleRun run);
}
