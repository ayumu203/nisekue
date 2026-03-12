using System.Text.Json;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.infrastructure.quest;

internal static class QuestJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string SerializeMoveSet(MoveSet moveSet)
    {
        return JsonSerializer.Serialize(moveSet.Slots.Select(x => x?.Id).ToArray(), Options);
    }

    public static MoveSet DeserializeMoveSet(string json)
    {
        var slots = JsonSerializer.Deserialize<int?[]>(json, Options) ?? [];
        return new MoveSet(slots.Select(x => x is null ? null : new MoveId(x.Value)));
    }

    public static string SerializeEffects(IEnumerable<BattleAilmentState> ailments, IEnumerable<BattleBuffState> buffs)
    {
        return JsonSerializer.Serialize(new EffectsDto(
            ailments.Select(x => new AilmentDto((int)x.Type, x.RemainingTurns, x.TriggerDamage is null ? null : new DamageDto(
                x.TriggerDamage.HitCount,
                x.TriggerDamage.PowerRate,
                x.TriggerDamage.FixedValue,
                x.TriggerDamage.CriticalRate,
                (int)x.TriggerDamage.ElementType,
                x.TriggerDamage.AttackStat is null ? null : (int)x.TriggerDamage.AttackStat.Value))).ToArray(),
            buffs.Select(x => new BuffDto((int)x.Stat, (int)x.CalculationType, x.Value, x.RemainingTurns)).ToArray()), Options);
    }

    public static (BattleAilmentState[] Ailments, BattleBuffState[] Buffs) DeserializeEffects(string json)
    {
        var dto = JsonSerializer.Deserialize<EffectsDto>(json, Options) ?? new EffectsDto([], []);
        return (
            dto.Ailments.Select(x => new BattleAilmentState(
                (AilmentType)x.Type,
                x.RemainingTurns,
                x.TriggerDamage is null
                    ? null
                    : new DamageEffect(
                        x.TriggerDamage.HitCount,
                        x.TriggerDamage.PowerRate,
                        x.TriggerDamage.FixedValue,
                        x.TriggerDamage.CriticalRate,
                        (ElementType)x.TriggerDamage.ElementType,
                        x.TriggerDamage.AttackStat is null ? null : (BuffStat)x.TriggerDamage.AttackStat.Value))).ToArray(),
            dto.Buffs.Select(x => new BattleBuffState(
                (BuffStat)x.Stat,
                (BuffCalculationType)x.CalculationType,
                x.Value,
                x.RemainingTurns)).ToArray());
    }

    public static string SerializeChatMessages(IEnumerable<QuestChatMessage> messages)
    {
        return JsonSerializer.Serialize(messages.Select(x => new ChatMessageDto(
            x.SenderParticipantId.Value,
            x.DisplayName,
            x.ImagePath,
            x.Message,
            x.SentAt)).ToArray(), Options);
    }

    public static QuestChatMessage[] DeserializeChatMessages(string json)
    {
        var messages = JsonSerializer.Deserialize<ChatMessageDto[]>(json, Options) ?? [];
        return messages.Select(x => new QuestChatMessage(
            new QuestParticipantId(x.SenderParticipantId),
            x.DisplayName,
            x.ImagePath,
            x.Message,
            x.SentAt)).ToArray();
    }

    public static string? SerializeLastTurnResults(QuestLastTurnResults? results)
    {
        return results is null
            ? null
            : JsonSerializer.Serialize(new LastTurnResultsDto(
                results.TurnNo,
                results.ResolvedAt,
                results.Actions.Select(action => new ResolvedActionDto(
                    action.ActorParticipantId,
                    action.ActorEnemyInstanceId,
                    action.ActorDisplayName,
                    action.ActionKind,
                    action.MoveId,
                    action.MoveName,
                    action.Succeeded,
                    action.TargetSummaries.Select(target => new ResolvedTargetSummaryDto(
                        target.TargetParticipantId,
                        target.TargetEnemyInstanceId,
                        target.TargetDisplayName,
                        target.ResultType,
                        target.HpChange,
                        target.MpChange,
                        target.AppliedEffects.ToArray(),
                        target.RemovedEffects.ToArray(),
                        target.IsDeadAfterAction)).ToArray(),
                    action.Logs.ToArray())).ToArray(),
                results.FloorTransition is null
                    ? null
                    : new FloorTransitionDto(
                        results.FloorTransition.PreviousFloorNo,
                        results.FloorTransition.CurrentFloorNo,
                        results.FloorTransition.FloorCleared,
                        results.FloorTransition.BossFloorReached),
                results.RunTransition is null
                    ? null
                    : new RunTransitionDto(
                        results.RunTransition.PreviousStatus,
                        results.RunTransition.CurrentStatus,
                        results.RunTransition.QuestEnded)), Options);
    }

    public static QuestLastTurnResults? DeserializeLastTurnResults(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var dto = JsonSerializer.Deserialize<LastTurnResultsDto>(json, Options);
        return dto is null
            ? null
            : new QuestLastTurnResults(
                dto.TurnNo,
                dto.ResolvedAt,
                dto.Actions.Select(action => new QuestResolvedAction(
                    action.ActorParticipantId,
                    action.ActorEnemyInstanceId,
                    action.ActorDisplayName,
                    action.ActionKind,
                    action.MoveId,
                    action.MoveName,
                    action.Succeeded,
                    action.TargetSummaries.Select(target => new QuestResolvedTargetSummary(
                        target.TargetParticipantId,
                        target.TargetEnemyInstanceId,
                        target.TargetDisplayName,
                        target.ResultType,
                        target.HpChange,
                        target.MpChange,
                        target.AppliedEffects,
                        target.RemovedEffects,
                        target.IsDeadAfterAction)),
                    action.Logs)).ToArray(),
                dto.FloorTransition is null
                    ? null
                    : new QuestFloorTransition(
                        dto.FloorTransition.PreviousFloorNo,
                        dto.FloorTransition.CurrentFloorNo,
                        dto.FloorTransition.FloorCleared,
                        dto.FloorTransition.BossFloorReached),
                dto.RunTransition is null
                    ? null
                    : new QuestRunTransition(
                        dto.RunTransition.PreviousStatus,
                        dto.RunTransition.CurrentStatus,
                        dto.RunTransition.QuestEnded));
    }

    public static string SerializeDerivedParametersPlaceholder()
    {
        return "{}";
    }

    private sealed record EffectsDto(AilmentDto[] Ailments, BuffDto[] Buffs);
    private sealed record AilmentDto(int Type, int RemainingTurns, DamageDto? TriggerDamage);
    private sealed record DamageDto(int HitCount, decimal PowerRate, int FixedValue, decimal CriticalRate, int ElementType, int? AttackStat);
    private sealed record BuffDto(int Stat, int CalculationType, decimal Value, int RemainingTurns);
    private sealed record ChatMessageDto(Guid SenderParticipantId, string DisplayName, string? ImagePath, string Message, DateTimeOffset SentAt);
    private sealed record LastTurnResultsDto(
        int TurnNo,
        DateTimeOffset ResolvedAt,
        ResolvedActionDto[] Actions,
        FloorTransitionDto? FloorTransition,
        RunTransitionDto? RunTransition);
    private sealed record ResolvedActionDto(
        Guid? ActorParticipantId,
        Guid? ActorEnemyInstanceId,
        string ActorDisplayName,
        string ActionKind,
        int? MoveId,
        string? MoveName,
        bool Succeeded,
        ResolvedTargetSummaryDto[] TargetSummaries,
        string[] Logs);
    private sealed record ResolvedTargetSummaryDto(
        Guid? TargetParticipantId,
        Guid? TargetEnemyInstanceId,
        string TargetDisplayName,
        string ResultType,
        int HpChange,
        int MpChange,
        string[] AppliedEffects,
        string[] RemovedEffects,
        bool IsDeadAfterAction);
    private sealed record FloorTransitionDto(int PreviousFloorNo, int CurrentFloorNo, bool FloorCleared, bool BossFloorReached);
    private sealed record RunTransitionDto(string PreviousStatus, string CurrentStatus, bool QuestEnded);
}
