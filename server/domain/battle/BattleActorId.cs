namespace server.domain.battle;

public readonly record struct BattleActorId(Guid Value)
{
    public override string ToString() => Value.ToString();
}
