using server.domain.battle.enums;
using server.domain.player;

namespace server.domain.quest;

/// <summary>名鑑の祠で味方 NPC として加えるプレイヤー。</summary>
public sealed record QuestPlayerNpc(PlayerId PlayerId, string DisplayName, BattleRow PreferredRow);
