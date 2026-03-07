namespace server.domain.training;

public interface ITrainingEnemyRepository
{
    Task<TrainingEnemy?> GetTrainingEnemyAsync(TrainingEnemyId id);
}
