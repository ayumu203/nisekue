namespace server.application.training;

public sealed record TrainingEnemyView(
    int Id,
    string Name,
    string ImagePath,
    int Level);
