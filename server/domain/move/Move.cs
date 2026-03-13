using server.domain.move.enums;
using server.shared.constants.move;

namespace server.domain.move;

public class Move
{
    private readonly List<MoveEffect> _effects = [];

    public MoveId Id { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string? EffectImagePath { get; private set; }
    public TargetType TargetType { get; private set; }
    public AttackRange AttackRange { get; private set; }
    public int MpCost { get; private set; }
    public int ExecutionPriority { get; private set; }
    public MoveCategory Category { get; private set; }
    public IReadOnlyList<MoveEffect> Effects => _effects;

    public Move(MoveId id, string name, string description, TargetType targetType, AttackRange attackRange, int mpCost, int executionPriority, MoveCategory category, string? effectImagePath = null)
        : this(id, name, description, targetType, attackRange, mpCost, executionPriority, category, effectImagePath, [])
    {
    }

    public Move(
        MoveId id,
        string name,
        string description,
        TargetType targetType,
        AttackRange attackRange,
        int mpCost,
        int executionPriority,
        MoveCategory category,
        string? effectImagePath = null,
        IEnumerable<MoveEffect>? effects = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = ValidateName(name);
        Description = ValidateDescription(description);
        EffectImagePath = ValidateEffectImagePath(effectImagePath);
        TargetType = targetType;
        AttackRange = attackRange;
        MpCost = ValidateMpCost(mpCost);
        ExecutionPriority = executionPriority;
        Category = category;

        if (effects is not null)
        {
            _effects.AddRange(effects);
        }

        Validate();
    }

    public IReadOnlyList<MoveEffect> GetOrderedEffects()
    {
        return _effects.OrderBy(x => x.Sequence).ToArray();
    }

    public void Validate()
    {
        if (_effects.Count == 0)
        {
            throw new InvalidOperationException("技には最低1つの効果が必要です。");
        }

        foreach (var effect in _effects)
        {
            if (effect.MoveId.Id != Id.Id)
            {
                throw new InvalidOperationException("MoveEffect の MoveId が Move と一致しません。");
            }

            effect.ValidateByType();
        }

        if (_effects.Select(x => x.Sequence).Distinct().Count() != _effects.Count)
        {
            throw new InvalidOperationException("同じ技に同一 sequence の効果は設定できません。");
        }

        if (_effects.Select(x => x.EffectId.Id).Distinct().Count() != _effects.Count)
        {
            throw new InvalidOperationException("同じ技に同一 EffectId の効果は設定できません。");
        }
    }

    private static string ValidateName(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 or > MoveConstants.Constraints.NameMaxLength)
        {
            throw new ArgumentException(
                $"技名は1文字から{MoveConstants.Constraints.NameMaxLength}文字以内です。",
                nameof(name));
        }

        return normalized;
    }

    private static int ValidateMpCost(int mpCost)
    {
        if (mpCost < MoveConstants.Constraints.MinMpCost)
        {
            throw new ArgumentOutOfRangeException(nameof(mpCost), "消費MPに負数は設定できません。");
        }

        return mpCost;
    }

    private static string? ValidateEffectImagePath(string? effectImagePath)
    {
        if (string.IsNullOrWhiteSpace(effectImagePath))
        {
            return null;
        }

        var normalized = effectImagePath.Trim();
        if (normalized.Length > MoveConstants.Constraints.ImagePathMaxLength)
        {
            throw new ArgumentException(
                $"技エフェクト画像パスは{MoveConstants.Constraints.ImagePathMaxLength}文字以内です。",
                nameof(effectImagePath));
        }

        return normalized;
    }

    private static string ValidateDescription(string description)
    {
        var normalized = description?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 or > MoveConstants.Constraints.DescriptionMaxLength)
        {
            throw new ArgumentException(
                $"技説明は1文字から{MoveConstants.Constraints.DescriptionMaxLength}文字以内です。",
                nameof(description));
        }

        return normalized;
    }
}
