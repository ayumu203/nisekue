namespace server.domain.battle.enums;

public enum BattleActionFailureReason
{
    ActorUnavailable = 1,
    CannotAct = 2,
    Paralyzed = 3,
    Sleeping = 4,
    NoTarget = 5,
    MoveUnavailable = 6,
    InsufficientMp = 7
}
