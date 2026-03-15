namespace server.domain.battle.enums;

public enum BattleActionFailureReason
{
    ActorUnavailable = 1,
    CannotAct = 2,
    Paralyzed = 3,
    NoTarget = 4,
    MoveUnavailable = 5,
    InsufficientMp = 6
}
