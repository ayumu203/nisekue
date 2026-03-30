using server.domain.player;
using server.shared.constants.player;

namespace server.application.player;

public class PlayerRebirthService(IPlayerRepository playerRepository)
{
    public async Task<Player> RebirthAsync(PlayerId playerId)
    {
        var player = await playerRepository.GetPlayerAsync(playerId)
            ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");

        player.Rebirth(BuildInheritedStatus(player.Status));
        await playerRepository.SaveAsync(player);
        return player;
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
            accuracy: currentStatus.Accuracy);
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
