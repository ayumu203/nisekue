namespace server.domain.quest.enums;

public enum ActionKind
{
    NormalAttack = 1,
    UseMove = 2,
    Guard = 3,
    Wait = 4,
    LeaveQuest = 5,
    Escape = 6,
    Prayer = 7,
    Capture = 8,
    SummonPet = 9,
    // ペット出撃中、プレイヤーが「ペットにまかせる」でターンを進める
    EntrustPet = 10
}
