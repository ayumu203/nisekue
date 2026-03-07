namespace server.domain.training;

public readonly record struct TrainingEnemyId(int Value)
{
    public override string ToString() => Value.ToString();
}
