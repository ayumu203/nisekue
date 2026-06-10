using System.Text.Json;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.pet;
using server.domain.move.enums;
using server.domain.pet_battle;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using server.infrastructure.quest;

namespace server.infrastructure.pet_battle;

internal static class PetBattleJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string SerializeSlots(IEnumerable<PetBattleRoomSlot> slots) =>
        JsonSerializer.Serialize(slots.Select(s => new SlotDto(s.PetId.Value, (int)s.Row, (int)s.Column)).ToArray(), Options);

    public static PetBattleRoomSlot[] DeserializeSlots(string json)
    {
        var dtos = JsonSerializer.Deserialize<SlotDto[]>(json, Options) ?? [];
        return dtos.Select(d => new PetBattleRoomSlot(new PlayerPetId(d.PetId), (BattleRow)d.Row, (BattleColumn)d.Column)).ToArray();
    }

    public static string SerializeSnapshots(IEnumerable<PetBattlePartyMemberSnapshot> snapshots) =>
        JsonSerializer.Serialize(snapshots.Select(s => new SnapshotDto(
            s.ParticipantId.Value,
            s.EnemyDefinitionId,
            s.DisplayName,
            s.ImagePath,
            new StatusDto(s.BaseStatus.MaxHp, s.BaseStatus.MaxMp, s.BaseStatus.Strength, s.BaseStatus.Defense, s.BaseStatus.Intelligence, s.BaseStatus.Luck, s.BaseStatus.Speed),
            s.MoveIds.ToArray(),
            (int)s.StartRow,
            (int)s.StartColumn)).ToArray(), Options);

    public static PetBattlePartyMemberSnapshot[] DeserializeSnapshots(string json)
    {
        var dtos = JsonSerializer.Deserialize<SnapshotDto[]>(json, Options) ?? [];
        return dtos.Select(d => new PetBattlePartyMemberSnapshot(
            new PetBattleParticipantId(d.ParticipantId),
            d.EnemyDefinitionId,
            d.DisplayName,
            d.ImagePath,
            new Status(d.Status.MaxHp, d.Status.MaxMp, d.Status.Strength, d.Status.Defense, d.Status.Intelligence, d.Status.Luck, d.Status.Speed),
            d.MoveIds,
            (BattleRow)d.StartRow,
            (BattleColumn)d.StartColumn)).ToArray();
    }

    public static string SerializeMembers(IEnumerable<PetBattlePartyMemberState> members) =>
        JsonSerializer.Serialize(members.Select(m => new MemberDto(
            m.ParticipantId.Value,
            m.CurrentHp,
            m.CurrentMp,
            m.IsDead,
            m.CanActFromTurn,
            (int)m.ActionMode,
            QuestJsonSerializer.SerializeEffects(m.Ailments, m.Buffs))).ToArray(), Options);

    public static PetBattlePartyMemberState[] DeserializeMembers(string json)
    {
        var dtos = JsonSerializer.Deserialize<MemberDto[]>(json, Options) ?? [];
        return dtos.Select(d =>
        {
            var (ailments, buffs) = QuestJsonSerializer.DeserializeEffects(d.EffectsJson);
            return new PetBattlePartyMemberState(
                new PetBattleParticipantId(d.ParticipantId),
                d.CurrentHp,
                d.CurrentMp,
                d.IsDead,
                d.CanActFromTurn,
                (ActionMode)d.ActionMode,
                ailments,
                buffs);
        }).ToArray();
    }

    public static string? SerializeLastTurnResults(PetBattleLastTurnResults? results)
    {
        if (results is null) return null;
        return JsonSerializer.Serialize(new LastTurnResultsDto(
            results.TurnNo,
            results.ResolvedAt,
            results.Actions.Select(a => new ResolvedActionDto(
                a.ActorParticipantId,
                a.ActorEnemyInstanceId,
                a.ActorDisplayName,
                a.ActionKind,
                a.MoveId,
                a.MoveName,
                a.Succeeded,
                a.TargetSummaries.Select(t => new ResolvedTargetDto(
                    t.TargetParticipantId,
                    t.TargetEnemyInstanceId,
                    t.TargetDisplayName,
                    t.ResultType,
                    t.HpChange,
                    t.MpChange,
                    t.AppliedEffects.ToArray(),
                    t.RemovedEffects.ToArray(),
                    t.IsDeadAfterAction)).ToArray(),
                a.Logs.ToArray(),
                a.LogEntries.Select(e => new LogEntryDto(e.Text, e.Segments.Select(s => new LogSegmentDto(s.Text, s.Tone)).ToArray())).ToArray())).ToArray()), Options);
    }

    public static PetBattleLastTurnResults? DeserializeLastTurnResults(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        var dto = JsonSerializer.Deserialize<LastTurnResultsDto>(json, Options);
        if (dto is null) return null;

        return new PetBattleLastTurnResults(
            dto.TurnNo,
            dto.ResolvedAt,
            dto.Actions.Select(a => new QuestResolvedAction(
                a.ActorParticipantId,
                a.ActorEnemyInstanceId,
                a.ActorDisplayName,
                a.ActionKind,
                a.MoveId,
                a.MoveName,
                a.Succeeded,
                a.TargetSummaries.Select(t => new QuestResolvedTargetSummary(
                    t.TargetParticipantId,
                    t.TargetEnemyInstanceId,
                    t.TargetDisplayName,
                    t.ResultType,
                    t.HpChange,
                    t.MpChange,
                    t.AppliedEffects,
                    t.RemovedEffects,
                    t.IsDeadAfterAction)),
                a.Logs,
                (a.LogEntries ?? []).Select(e => new QuestBattleLogEntry(e.Text, (e.Segments ?? []).Select(s => new QuestBattleLogSegment(s.Text, s.Tone)))).ToArray())).ToArray());
    }

    private sealed record SlotDto(Guid PetId, int Row, int Column);
    private sealed record StatusDto(int MaxHp, int MaxMp, int Strength, int Defense, int Intelligence, int Luck, int Speed);
    private sealed record SnapshotDto(Guid ParticipantId, int EnemyDefinitionId, string DisplayName, string? ImagePath, StatusDto Status, int[] MoveIds, int StartRow, int StartColumn);
    private sealed record MemberDto(Guid ParticipantId, int CurrentHp, int CurrentMp, bool IsDead, int CanActFromTurn, int ActionMode, string EffectsJson);
    private sealed record LastTurnResultsDto(int TurnNo, DateTimeOffset ResolvedAt, ResolvedActionDto[] Actions);
    private sealed record ResolvedActionDto(Guid? ActorParticipantId, Guid? ActorEnemyInstanceId, string ActorDisplayName, string ActionKind, int? MoveId, string? MoveName, bool Succeeded, ResolvedTargetDto[] TargetSummaries, string[] Logs, LogEntryDto[]? LogEntries);
    private sealed record ResolvedTargetDto(Guid? TargetParticipantId, Guid? TargetEnemyInstanceId, string TargetDisplayName, string ResultType, int HpChange, int MpChange, string[] AppliedEffects, string[] RemovedEffects, bool IsDeadAfterAction);
    private sealed record LogEntryDto(string Text, LogSegmentDto[] Segments);
    private sealed record LogSegmentDto(string Text, string Tone);
}
