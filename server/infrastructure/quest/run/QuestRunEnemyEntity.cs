namespace server.infrastructure.quest.run;

public class QuestRunEnemyEntity
{
    public Guid RunId { get; set; }
    public Guid EnemyInstanceId { get; set; }
    public int FloorNo { get; set; }
    public int EnemyDefinitionId { get; set; }
    public int BattleRow { get; set; }
    public int BattleColumn { get; set; }
    public int CurrentHp { get; set; }
    public int CurrentMp { get; set; }
    public bool IsDead { get; set; }
    public bool IsCaptured { get; set; }
    public string ActiveEffectsJson { get; set; } = "{}";
    public string DerivedParametersJson { get; set; } = "{}";
}
