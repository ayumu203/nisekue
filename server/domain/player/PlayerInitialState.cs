using server.domain.move;

namespace server.domain.player;

/// <summary>
/// プレイヤー作成時の初期値。プレイヤー作成とデバッグ機能の「作成直後に戻す」で共有する。
/// </summary>
public static class PlayerInitialState
{
    public const int Gold = 100;

    private static readonly MoveId InitialMoveId = new(101);

    public static Status CreateStatus()
    {
        return new Status(maxHp: 24, maxMp: 8, strength: 7, defense: 5, intelligence: 5, luck: 3, speed: 4);
    }

    public static MoveSet CreateMoveSet()
    {
        var moveSet = new MoveSet();
        moveSet.SetSlot(0, InitialMoveId);
        return moveSet;
    }
}
