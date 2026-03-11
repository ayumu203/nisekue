using server.domain.battle;

namespace server.domain.quest;

public class QuestBattleState(
    IEnumerable<QuestRunPartyMemberState> partyMembers,
    IEnumerable<QuestEnemyState> enemies)
{
    private readonly QuestRunPartyMemberState[] partyMembers = partyMembers?.ToArray()
        ?? throw new ArgumentNullException(nameof(partyMembers));
    private readonly QuestEnemyState[] enemies = enemies?.ToArray()
        ?? throw new ArgumentNullException(nameof(enemies));

    public IReadOnlyList<QuestRunPartyMemberState> PartyMembers => partyMembers;
    public IReadOnlyList<QuestEnemyState> Enemies => enemies;

    public QuestRunPartyMemberState FindPartyMember(QuestParticipantId participantId)
    {
        return partyMembers.FirstOrDefault(x => x.ParticipantId == participantId)
            ?? throw new KeyNotFoundException("参加者状態が見つかりません。");
    }

    public bool HasContinuablePartyMember()
    {
        return partyMembers.Any(x => x.IsContinuable());
    }

    public bool AreAllEnemiesDefeated()
    {
        return enemies.All(x => !x.IsAlive);
    }

    public void ApplyResolution(
        BattleTurnResolution resolution,
        IReadOnlyDictionary<BattleActorId, QuestParticipantId> partyActorMap,
        IReadOnlyDictionary<BattleActorId, QuestEnemyInstanceId> enemyActorMap,
        int currentTurnNo)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(partyActorMap);
        ArgumentNullException.ThrowIfNull(enemyActorMap);

        var partyById = partyMembers.ToDictionary(x => x.ParticipantId);
        var enemyById = enemies.ToDictionary(x => x.Id);

        foreach (var state in resolution.UpdatedStates)
        {
            if (partyActorMap.TryGetValue(state.Id, out var participantId))
            {
                partyById[participantId].ApplyBattleState(state, currentTurnNo);
                continue;
            }

            if (enemyActorMap.TryGetValue(state.Id, out var enemyInstanceId))
            {
                enemyById[enemyInstanceId].ApplyBattleState(state);
            }
        }
    }
}
