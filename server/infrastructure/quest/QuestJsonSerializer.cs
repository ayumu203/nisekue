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
        return JsonSerializer.Serialize(ToEffectsDto(ailments, buffs), Options);
    }

    public static (BattleAilmentState[] Ailments, BattleBuffState[] Buffs) DeserializeEffects(string json)
    {
        var dto = JsonSerializer.Deserialize<EffectsDto>(json, Options) ?? new EffectsDto([], []);
        return FromEffectsDto(dto);
    }

    public static string SerializePets(IEnumerable<QuestPetSnapshot> pets)
    {
        return JsonSerializer.Serialize(pets.Select(x => new PetSnapshotDto(x.EnemyDefinitionId.Value, ToStatusDto(x.Status))).ToArray(), Options);
    }

    public static QuestPetSnapshot[] DeserializePets(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var dtos = JsonSerializer.Deserialize<PetSnapshotDto[]>(json, Options) ?? [];
        return dtos
            .Select(x => new QuestPetSnapshot(new QuestEnemyDefinitionId(x.EnemyDefinitionId), FromStatusDto(x.Status)))
            .ToArray();
    }

    public static string? SerializePetDeployment(QuestPetDeployment? deployment)
    {
        if (deployment is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(new PetDeploymentDto(
            deployment.RemainingTurns,
            deployment.Pets.Select(x => new DeployedPetDto(
                x.ActorId,
                x.OwnerParticipantId.Value,
                x.EnemyDefinitionId.Value,
                ToStatusDto(x.Status),
                (int)x.Position.Row,
                (int)x.Position.Column,
                x.CurrentHp,
                x.IsDead,
                ToEffectsDto(x.Ailments, x.Buffs))).ToArray()), Options);
    }

    public static QuestPetDeployment? DeserializePetDeployment(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var dto = JsonSerializer.Deserialize<PetDeploymentDto>(json, Options);
        if (dto is null)
        {
            return null;
        }

        return new QuestPetDeployment(
            dto.RemainingTurns,
            (dto.Pets ?? []).Select(x =>
            {
                var (ailments, buffs) = FromEffectsDto(x.Effects ?? new EffectsDto([], []));
                return new QuestDeployedPetState(
                    x.ActorId,
                    new QuestParticipantId(x.OwnerParticipantId),
                    new QuestEnemyDefinitionId(x.EnemyDefinitionId),
                    FromStatusDto(x.Status),
                    new BattlePosition((BattleRow)x.Row, (BattleColumn)x.Column),
                    x.CurrentHp,
                    x.IsDead,
                    ailments,
                    buffs);
            }).ToArray());
    }

    private static StatusDto ToStatusDto(Status status)
    {
        return new StatusDto(status.MaxHp, status.MaxMp, status.Strength, status.Defense, status.Intelligence, status.Luck, status.Speed);
    }

    private static Status FromStatusDto(StatusDto dto)
    {
        return new Status(dto.MaxHp, dto.MaxMp, dto.Strength, dto.Defense, dto.Intelligence, dto.Luck, dto.Speed);
    }

    private static EffectsDto ToEffectsDto(IEnumerable<BattleAilmentState> ailments, IEnumerable<BattleBuffState> buffs)
    {
        return new EffectsDto(
            ailments.Select(x => new AilmentDto((int)x.Type, x.RemainingTurns, x.SourceMoveId?.Id, x.MaxHpLimit, x.TriggerDamage is null ? null : new DamageDto(
                x.TriggerDamage.HitCount,
                x.TriggerDamage.PowerRate,
                x.TriggerDamage.FixedValue,
                x.TriggerDamage.CriticalRate,
                (int)x.TriggerDamage.ElementType,
                x.TriggerDamage.AttackStat is null ? null : (int)x.TriggerDamage.AttackStat.Value))).ToArray(),
            buffs.Select(x => new BuffDto((int)x.Stat, (int)x.CalculationType, x.Value, x.RemainingTurns)).ToArray());
    }

    private static (BattleAilmentState[] Ailments, BattleBuffState[] Buffs) FromEffectsDto(EffectsDto dto)
    {
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
                        x.TriggerDamage.AttackStat is null ? null : (BuffStat)x.TriggerDamage.AttackStat.Value),
                x.SourceMoveId is null ? null : new MoveId(x.SourceMoveId.Value),
                x.MaxHpLimit)).ToArray(),
            dto.Buffs.Select(x => new BattleBuffState(
                (BuffStat)x.Stat,
                (BuffCalculationType)x.CalculationType,
                x.Value,
                x.RemainingTurns)).ToArray());
    }

    public static string SerializeChatMessages(IEnumerable<QuestChatMessage> messages)
    {
        return JsonSerializer.Serialize(messages.Select(x => new ChatMessageDto(
            x.TurnNo,
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
            x.TurnNo <= 0 ? 1 : x.TurnNo,
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
                results.ChatMessages.Select(message => new ChatMessageDto(
                    message.TurnNo,
                    message.SenderParticipantId.Value,
                    message.DisplayName,
                    message.ImagePath,
                    message.Message,
                    message.SentAt)).ToArray(),
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
                    action.Logs.ToArray(),
                    action.LogEntries.Select(entry => new BattleLogEntryDto(
                        entry.Text,
                        entry.Segments.Select(segment => new BattleLogSegmentDto(segment.Text, segment.Tone)).ToArray())).ToArray())).ToArray(),
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
                (dto.ChatMessages ?? []).Select(message => new QuestChatMessage(
                    message.TurnNo <= 0 ? dto.TurnNo : message.TurnNo,
                    new QuestParticipantId(message.SenderParticipantId),
                    message.DisplayName,
                    message.ImagePath,
                    message.Message,
                    message.SentAt)).ToArray(),
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
                    action.Logs,
                    (action.LogEntries ?? [])
                        .Select(entry => new QuestBattleLogEntry(
                            entry.Text,
                            (entry.Segments ?? []).Select(segment => new QuestBattleLogSegment(segment.Text, segment.Tone))))
                        .ToArray()))
                    .ToArray(),
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
    private sealed record StatusDto(int MaxHp, int MaxMp, int Strength, int Defense, int Intelligence, int Luck, int Speed);
    private sealed record PetSnapshotDto(int EnemyDefinitionId, StatusDto Status);
    private sealed record PetDeploymentDto(int RemainingTurns, DeployedPetDto[]? Pets);
    private sealed record DeployedPetDto(
        Guid ActorId,
        Guid OwnerParticipantId,
        int EnemyDefinitionId,
        StatusDto Status,
        int Row,
        int Column,
        int CurrentHp,
        bool IsDead,
        EffectsDto? Effects);
    private sealed record AilmentDto(int Type, int RemainingTurns, int? SourceMoveId, int? MaxHpLimit, DamageDto? TriggerDamage);
    private sealed record DamageDto(int HitCount, decimal PowerRate, int FixedValue, decimal CriticalRate, int ElementType, int? AttackStat);
    private sealed record BuffDto(int Stat, int CalculationType, decimal Value, int RemainingTurns);
    private sealed record ChatMessageDto(int TurnNo, Guid SenderParticipantId, string DisplayName, string? ImagePath, string Message, DateTimeOffset SentAt);
    private sealed record LastTurnResultsDto(
        int TurnNo,
        DateTimeOffset ResolvedAt,
        ChatMessageDto[]? ChatMessages,
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
        string[] Logs,
        BattleLogEntryDto[]? LogEntries);
    private sealed record BattleLogEntryDto(string Text, BattleLogSegmentDto[] Segments);
    private sealed record BattleLogSegmentDto(string Text, string Tone);
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
