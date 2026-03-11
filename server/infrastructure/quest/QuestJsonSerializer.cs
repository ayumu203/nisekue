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

    public static string SerializeDerivedParametersPlaceholder()
    {
        return "{}";
    }

    private sealed record EffectsDto(AilmentDto[] Ailments, BuffDto[] Buffs);
    private sealed record AilmentDto(int Type, int RemainingTurns, DamageDto? TriggerDamage);
    private sealed record DamageDto(int HitCount, decimal PowerRate, int FixedValue, decimal CriticalRate, int ElementType, int? AttackStat);
    private sealed record BuffDto(int Stat, int CalculationType, decimal Value, int RemainingTurns);
    private sealed record ChatMessageDto(Guid SenderParticipantId, string DisplayName, string? ImagePath, string Message, DateTimeOffset SentAt);
}
