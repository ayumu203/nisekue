using server.domain.player;
using server.shared.constants.player;

namespace server.application.player;

public class PlayerRebirthService(
    IPlayerRepository playerRepository,
    IPlayerMutationService? playerMutationService = null)
{
    public async Task<Player> RebirthAsync(PlayerId playerId)
    {
        if (playerMutationService is null)
        {
            var player = await playerRepository.GetPlayerAsync(playerId)
                ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");

            player.Rebirth(BuildInheritedStatus(player.Status));
            await playerRepository.SaveAsync(player);
            return player;
        }

        return await playerMutationService.MutateAsync(playerId, player =>
        {
            player.Rebirth(BuildInheritedStatus(player.Status));
            return Task.FromResult(player);
        });
    }

    private static Status BuildInheritedStatus(Status currentStatus)
    {
        ArgumentNullException.ThrowIfNull(currentStatus);

        return new Status(
            maxHp: InheritMaxHp(currentStatus.MaxHp),
            maxMp: InheritNonNegative(currentStatus.MaxMp),
            strength: InheritNonNegative(currentStatus.Strength),
            defense: InheritNonNegative(currentStatus.Defense),
            intelligence: InheritNonNegative(currentStatus.Intelligence),
            luck: InheritNonNegative(currentStatus.Luck),
            speed: InheritNonNegative(currentStatus.Speed),
            accuracy: currentStatus.Accuracy,
            evasion: currentStatus.Evasion,
            criticalChance: currentStatus.CriticalChance,
            damageReduction: currentStatus.DamageReduction);
    }

    private static int InheritMaxHp(int currentValue)
    {
        return Math.Max(1, InheritValue(currentValue));
    }

    private static int InheritNonNegative(int currentValue)
    {
        return Math.Max(0, InheritValue(currentValue));
    }

    private static int InheritValue(int currentValue)
    {
        var rate = Random.Shared.Next(
            PlayerConstants.RebirthInheritanceRateMin,
            PlayerConstants.RebirthInheritanceRateMax + 1);
        return currentValue * rate / 100;
    }
}
