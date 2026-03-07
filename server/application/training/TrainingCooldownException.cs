namespace server.application.training;

public sealed class TrainingCooldownException(DateTimeOffset cooldownUntil)
    : Exception("トレーニングの待機時間中です。")
{
    public DateTimeOffset CooldownUntil { get; } = cooldownUntil;
}
