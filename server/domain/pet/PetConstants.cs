namespace server.domain.pet;

public static class PetConstants
{
    public const int MaxPetCount = 3;
    public const int TrainingCostGold = 100000;
    public const int TrainingRatePercent = 1;
    // 呼出で出撃したペットが戦うターン数（呼出ターンを 1 ターン目と数える）
    public const int DeploymentTurns = 3;
}
