using server.domain.player;

namespace server.infrastructure.player;

public class PlayerEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public Job Job { get; set; } = Job.Apprentice;
    public int RebirthCount { get; set; }
    public int Level { get; set; }
    public int Exp { get; set; }
    public int JobLevel { get; set; }

    /// <summary>廃止済み。職業レベルはプレイヤーレベルに連動するため未使用で、常に 0 を書き込む。列自体は未削除。</summary>
    public int JobExp { get; set; }
    public int Gold { get; set; }
    public int MaxHp { get; set; }
    public int MaxMp { get; set; }
    public int Strength { get; set; }
    public int Defense { get; set; }
    public int Intelligence { get; set; }
    public int Luck { get; set; }
    public int Speed { get; set; }
    public int TrainingBattleCount { get; set; }
    public DateTimeOffset? TrainingCooldownUntil { get; set; }
    public DateTimeOffset? QuestCooldownUntil { get; set; }
    public DateTimeOffset? PetBattleCooldownUntil { get; set; }
    public int ExpMultiplierFlags { get; set; }
    public int MapUnlockFlags { get; set; }
    public long RoadmapUnlockFlags { get; set; }
    public int EndlessBestFloor { get; set; }
    public DateTimeOffset? LastActiveAt { get; set; }
}
