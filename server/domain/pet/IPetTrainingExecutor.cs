using server.domain.player;

namespace server.domain.pet;

/// <summary>
/// 育成（ゴールド消費＋ペット加算）を原子的に実行する。並行リクエストでも支払った回数分だけ育成されることを保証する。
/// </summary>
public interface IPetTrainingExecutor
{
    Task<PlayerPet> TrainAsync(PlayerId playerId, PlayerPetId petId);
}
