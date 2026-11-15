using server.domain.battle.enums;
using server.domain.player;

namespace server.domain.quest;

/// <summary>名鑑の祠で敵として出るプレイヤーの職業系統。</summary>
public enum MirrorJobGroup
{
    Apprentice = 1,
    Warrior = 2,
    Guardian = 3,
    Mage = 4,
    Priest = 5,
    Ranger = 6
}

/// <summary>
/// 名鑑の祠の職業→系統・希望列。
/// 前衛＝戦士系・守護系（トリックスター以外）／中衛＝狩人系・見習い・トリックスター・ネクロマンサー／後衛＝魔法系・僧侶系（ネクロマンサー以外）。
/// 系統の分け方は味方 NPC の自動行動（QuestAllyNpcActionPolicy）と同じ。
/// </summary>
public static class MirrorJobRowPolicy
{
    public static MirrorJobGroup ResolveGroup(Job job)
    {
        return job switch
        {
            Job.Warrior or Job.OniWarrior or Job.SwordMaster or Job.GrandWarrior or Job.Shogun or Job.Bushin => MirrorJobGroup.Warrior,
            Job.Guardian or Job.Trickster or Job.Crusader or Job.GrandGuard or Job.GreatKnight or Job.Shugoshin => MirrorJobGroup.Guardian,
            Job.Mage or Job.FireMage or Job.WaterMage or Job.WindMage or Job.GrandCaster or Job.Archmage or Job.Seikaiou => MirrorJobGroup.Mage,
            Job.Priest or Job.HighPriest or Job.Necromancer or Job.GrandPriest => MirrorJobGroup.Priest,
            Job.Ranger or Job.Sniper or Job.TrapMaster or Job.GrandRanger or Job.GreatThief or Job.Matouou => MirrorJobGroup.Ranger,
            _ => MirrorJobGroup.Apprentice
        };
    }

    public static BattleRow ResolvePreferredRow(Job job)
    {
        if (job is Job.Trickster or Job.Necromancer)
        {
            return BattleRow.Middle;
        }

        return ResolveGroup(job) switch
        {
            MirrorJobGroup.Warrior or MirrorJobGroup.Guardian => BattleRow.Front,
            MirrorJobGroup.Mage or MirrorJobGroup.Priest => BattleRow.Back,
            _ => BattleRow.Middle
        };
    }
}
