using server.domain.player;
using server.domain.quest;

namespace server.domain.pet;

public class PlayerPet(
    PlayerPetId id,
    PlayerId playerId,
    QuestEnemyDefinitionId enemyDefinitionId,
    PetBonusStatus bonusStatus,
    bool isStandby,
    DateTimeOffset capturedAt,
    DateTimeOffset updatedAt)
{
    public PlayerPetId Id { get; } = id;
    public PlayerId PlayerId { get; } = playerId;
    public QuestEnemyDefinitionId EnemyDefinitionId { get; } = enemyDefinitionId;
    public PetBonusStatus BonusStatus { get; private set; } = bonusStatus ?? throw new ArgumentNullException(nameof(bonusStatus));
    public bool IsStandby { get; private set; } = isStandby;
    public DateTimeOffset CapturedAt { get; } = capturedAt;
    public DateTimeOffset UpdatedAt { get; private set; } = updatedAt;

    public static PlayerPet Capture(PlayerId playerId, QuestEnemyDefinitionId enemyDefinitionId, DateTimeOffset now)
    {
        return new PlayerPet(
            PlayerPetId.New(),
            playerId,
            enemyDefinitionId,
            new PetBonusStatus(),
            isStandby: false,
            capturedAt: now,
            updatedAt: now);
    }

    public void Train(Status playerBaseStatus, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(playerBaseStatus);

        BonusStatus = BonusStatus.Add(new PetBonusStatus(
            maxHp: TrainingGain(playerBaseStatus.MaxHp),
            maxMp: TrainingGain(playerBaseStatus.MaxMp),
            strength: TrainingGain(playerBaseStatus.Strength),
            defense: TrainingGain(playerBaseStatus.Defense),
            intelligence: TrainingGain(playerBaseStatus.Intelligence),
            luck: TrainingGain(playerBaseStatus.Luck),
            speed: TrainingGain(playerBaseStatus.Speed)));
        UpdatedAt = now;
    }

    public void Standby(DateTimeOffset now)
    {
        IsStandby = true;
        UpdatedAt = now;
    }

    public void ClearStandby(DateTimeOffset now)
    {
        IsStandby = false;
        UpdatedAt = now;
    }

    private static int TrainingGain(int statusValue)
    {
        return statusValue * PetConstants.TrainingRatePercent / 100;
    }
}
