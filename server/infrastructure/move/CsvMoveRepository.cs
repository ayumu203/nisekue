using System.Globalization;
using server.domain.move;
using server.domain.move.enums;

namespace server.infrastructure.move;

public class CsvMoveRepository : IMoveRepository
{
    private readonly IReadOnlyDictionary<int, Move> moveById;
    private readonly IReadOnlyList<Move> moves;

    public CsvMoveRepository()
    {
        var resourceDir = Path.Combine(AppContext.BaseDirectory, "resources");
        var moveMasterPath = Path.Combine(resourceDir, "move_master.csv");
        var moveEffectsPath = Path.Combine(resourceDir, "move_effects.csv");

        moveById = LoadMoves(moveMasterPath, moveEffectsPath);
        moves = moveById.Values.OrderBy(x => x.Id.Id).ToArray();
    }

    public Task<Move?> GetMoveAsync(MoveId moveId)
    {
        ArgumentNullException.ThrowIfNull(moveId);
        moveById.TryGetValue(moveId.Id, out var move);
        return Task.FromResult(move);
    }

    public Task<IReadOnlyList<Move>> GetAllMovesAsync()
    {
        return Task.FromResult(moves);
    }

    private static IReadOnlyDictionary<int, Move> LoadMoves(string moveMasterPath, string moveEffectsPath)
    {
        var masters = LoadMoveMasters(moveMasterPath);
        var effectsByMoveId = LoadMoveEffects(moveEffectsPath);
        var extraMoveIds = effectsByMoveId.Keys.Except(masters.Keys).OrderBy(x => x).ToArray();
        if (extraMoveIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"move_effects.csv に move_master.csv に存在しない move_id が含まれています。move_id={string.Join(", ", extraMoveIds)}");
        }

        var map = new Dictionary<int, Move>();

        foreach (var master in masters.Values)
        {
            if (!effectsByMoveId.TryGetValue(master.MoveId, out var effects))
            {
                throw new InvalidOperationException($"move_id={master.MoveId} の効果データが存在しません。");
            }

            var move = new Move(
                id: new MoveId(master.MoveId),
                name: master.MoveName,
                description: master.Description,
                targetType: master.TargetType,
                attackRange: master.AttackRange,
                mpCost: master.MpCost,
                executionPriority: master.ExecutionPriority,
                category: master.MoveCategory,
                effects: effects);

            map.Add(master.MoveId, move);
        }

        return map;
    }

    private static IReadOnlyDictionary<int, MoveMasterRow> LoadMoveMasters(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new InvalidOperationException($"CSVファイルが見つかりません: {csvPath}");
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException($"CSVファイルに技データがありません: {csvPath}");
        }

        var map = new Dictionary<int, MoveMasterRow>();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = line.Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length != 8)
            {
                throw new InvalidOperationException($"move_master.csv の形式が不正です。行: {i + 1}");
            }

            var moveId = ParseInt(columns[0], "move_id", i + 1);
            if (map.ContainsKey(moveId))
            {
                throw new InvalidOperationException($"move_master.csv で move_id が重複しています。move_id={moveId}, 行: {i + 1}");
            }

            map.Add(moveId, new MoveMasterRow(
                MoveId: moveId,
                MoveName: columns[1],
                Description: columns[2],
                TargetType: ParseEnum<TargetType>(columns[3], "target_type", i + 1),
                AttackRange: ParseEnum<AttackRange>(columns[4], "attack_range", i + 1),
                MpCost: ParseInt(columns[5], "mp_cost", i + 1),
                ExecutionPriority: ParseInt(columns[6], "execution_priority", i + 1),
                MoveCategory: ParseEnum<MoveCategory>(columns[7], "move_category", i + 1)));
        }

        if (map.Count == 0)
        {
            throw new InvalidOperationException($"move_master.csv に有効な技データがありません: {csvPath}");
        }

        return map;
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<MoveEffect>> LoadMoveEffects(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new InvalidOperationException($"CSVファイルが見つかりません: {csvPath}");
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException($"CSVファイルに技効果データがありません: {csvPath}");
        }

        var map = new Dictionary<int, List<MoveEffect>>();
        var usedEffectIds = new HashSet<int>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = line.Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length != 19)
            {
                throw new InvalidOperationException($"move_effects.csv の形式が不正です。行: {i + 1}");
            }

            var effectId = ParseInt(columns[0], "effect_id", i + 1);
            var moveId = ParseInt(columns[1], "move_id", i + 1);
            if (!usedEffectIds.Add(effectId))
            {
                throw new InvalidOperationException($"move_effects.csv で effect_id が重複しています。effect_id={effectId}, 行: {i + 1}");
            }

            var effectType = ParseEnum<MoveEffectType>(columns[3], "effect_type", i + 1);
            var damage = BuildDamageEffect(columns, effectType, i + 1);
            var ailment = BuildAilmentEffect(columns, effectType, i + 1);
            var buff = BuildBuffEffect(columns, effectType, i + 1);

            var effect = new MoveEffect(
                effectId: new MoveEffectId(effectId),
                moveId: new MoveId(moveId),
                sequence: ParseInt(columns[2], "sequence", i + 1),
                effectType: effectType,
                damage: damage,
                ailment: ailment,
                buff: buff);

            if (!map.TryGetValue(moveId, out var list))
            {
                list = [];
                map[moveId] = list;
            }

            list.Add(effect);
        }

        if (map.Count == 0)
        {
            throw new InvalidOperationException($"move_effects.csv に有効な技効果データがありません: {csvPath}");
        }

        return map.ToDictionary(
            x => x.Key,
            x => (IReadOnlyList<MoveEffect>)x.Value.OrderBy(effect => effect.Sequence).ToArray());
    }

    private static DamageEffect? BuildDamageEffect(string[] columns, MoveEffectType effectType, int lineNumber)
    {
        if (effectType is not (MoveEffectType.Damage or MoveEffectType.Heal))
        {
            return null;
        }

        var hitCount = ParseNullableInt(columns[4], "hit_count", lineNumber) ?? 1;
        var powerRate = ParseNullableDecimal(columns[5], "power_rate", lineNumber) ?? 1m;
        var fixedValue = ParseNullableInt(columns[6], "fixed_value", lineNumber) ?? 0;
        var criticalRate = ParseNullableDecimal(columns[7], "critical_rate", lineNumber) ?? 0m;
        var elementType = ParseNullableEnum<ElementType>(columns[8], "element_type", lineNumber) ?? ElementType.None;
        var attackStat = ParseNullableEnum<BuffStat>(columns[9], "attack_stat", lineNumber);

        return new DamageEffect(hitCount, powerRate, fixedValue, criticalRate, elementType, attackStat);
    }

    private static AilmentEffect? BuildAilmentEffect(string[] columns, MoveEffectType effectType, int lineNumber)
    {
        if (effectType != MoveEffectType.Ailment)
        {
            return null;
        }

        var ailmentType = ParseNullableEnum<AilmentType>(columns[10], "ailment_type", lineNumber)
            ?? throw new InvalidOperationException($"Ailment には ailment_type が必要です。行: {lineNumber}");
        var ailmentRate = ParseNullableDecimal(columns[11], "ailment_rate", lineNumber)
            ?? throw new InvalidOperationException($"Ailment には ailment_rate が必要です。行: {lineNumber}");
        var ailmentTurns = ParseNullableInt(columns[12], "ailment_turns", lineNumber)
            ?? throw new InvalidOperationException($"Ailment には ailment_turns が必要です。行: {lineNumber}");
        var triggerDamage = BuildAilmentTriggerDamage(columns, ailmentType, lineNumber);

        return new AilmentEffect(ailmentType, ailmentRate, ailmentTurns, triggerDamage);
    }

    private static DamageEffect? BuildAilmentTriggerDamage(string[] columns, AilmentType ailmentType, int lineNumber)
    {
        if (ailmentType != AilmentType.DamageTrap)
        {
            return null;
        }

        var hitCount = ParseNullableInt(columns[4], "hit_count", lineNumber) ?? 1;
        var powerRate = ParseNullableDecimal(columns[5], "power_rate", lineNumber) ?? 0m;
        var fixedValue = ParseNullableInt(columns[6], "fixed_value", lineNumber) ?? 0;
        var criticalRate = ParseNullableDecimal(columns[7], "critical_rate", lineNumber) ?? 0m;
        var elementType = ParseNullableEnum<ElementType>(columns[8], "element_type", lineNumber) ?? ElementType.None;
        var attackStat = ParseNullableEnum<BuffStat>(columns[9], "attack_stat", lineNumber);

        return new DamageEffect(hitCount, powerRate, fixedValue, criticalRate, elementType, attackStat);
    }

    private static BuffEffect? BuildBuffEffect(string[] columns, MoveEffectType effectType, int lineNumber)
    {
        if (effectType != MoveEffectType.Buff)
        {
            return null;
        }

        var buffStat = ParseNullableEnum<BuffStat>(columns[13], "buff_stat", lineNumber)
            ?? throw new InvalidOperationException($"Buff には buff_stat が必要です。行: {lineNumber}");
        var buffCalculationType = ParseNullableEnum<BuffCalculationType>(columns[14], "buff_op", lineNumber)
            ?? throw new InvalidOperationException($"Buff には buff_op が必要です。行: {lineNumber}");
        var buffValue = ParseNullableDecimal(columns[15], "buff_value", lineNumber)
            ?? throw new InvalidOperationException($"Buff には buff_value が必要です。行: {lineNumber}");
        var buffTurns = ParseNullableInt(columns[16], "buff_turns", lineNumber)
            ?? throw new InvalidOperationException($"Buff には buff_turns が必要です。行: {lineNumber}");
        var buffRate = ParseNullableDecimal(columns[17], "buff_rate", lineNumber)
            ?? throw new InvalidOperationException($"Buff には buff_rate が必要です。行: {lineNumber}");
        var canStack = ParseNullableBool(columns[18], "can_stack", lineNumber) ?? false;

        return new BuffEffect(buffStat, buffCalculationType, buffValue, buffTurns, buffRate, canStack);
    }

    private static int ParseInt(string value, string columnName, int lineNumber)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException($"CSVの数値変換に失敗しました。column: {columnName}, 行: {lineNumber}");
        }

        return parsed;
    }

    private static int? ParseNullableInt(string value, string columnName, int lineNumber)
    {
        return string.IsNullOrWhiteSpace(value) ? null : ParseInt(value, columnName, lineNumber);
    }

    private static decimal ParseDecimal(string value, string columnName, int lineNumber)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException($"CSVの小数変換に失敗しました。column: {columnName}, 行: {lineNumber}");
        }

        return parsed;
    }

    private static decimal? ParseNullableDecimal(string value, string columnName, int lineNumber)
    {
        return string.IsNullOrWhiteSpace(value) ? null : ParseDecimal(value, columnName, lineNumber);
    }

    private static bool? ParseNullableBool(string value, string columnName, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!bool.TryParse(value, out var parsed))
        {
            throw new InvalidOperationException($"CSVのbool変換に失敗しました。column: {columnName}, 行: {lineNumber}");
        }

        return parsed;
    }

    private static T ParseEnum<T>(string value, string columnName, int lineNumber) where T : struct
    {
        if (!Enum.TryParse<T>(value, ignoreCase: false, out var parsed))
        {
            throw new InvalidOperationException($"CSVのenum変換に失敗しました。column: {columnName}, value: {value}, 行: {lineNumber}");
        }

        return parsed;
    }

    private static T? ParseNullableEnum<T>(string value, string columnName, int lineNumber) where T : struct
    {
        return string.IsNullOrWhiteSpace(value) ? null : ParseEnum<T>(value, columnName, lineNumber);
    }

    private readonly record struct MoveMasterRow(
        int MoveId,
        string MoveName,
        string Description,
        TargetType TargetType,
        AttackRange AttackRange,
        int MpCost,
        int ExecutionPriority,
        MoveCategory MoveCategory);
}
