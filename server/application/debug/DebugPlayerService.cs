using server.application.player;
using server.domain.move;
using server.domain.player;
using server.shared.constants.player;

namespace server.application.debug;

/// <summary>
/// デバッグ機能で、プレイヤーのデータを直接書き換える。
/// 利用可否の判定は呼び出し側（DebugEndpoints のフィルター）で行う。
/// </summary>
public class DebugPlayerService(
    IPlayerMutationService playerMutationService,
    IMoveRepository moveRepository,
    IJobProfileRepository jobProfileRepository,
    IJobMoveLearningRuleRepository jobMoveLearningRuleRepository)
{
    /// <summary>基本ステータス（HP・MP・筋力・防御・知力・運・素早さ）を指定した値にする。</summary>
    public Task<Player> UpdateStatusAsync(PlayerId playerId, DebugStatusValues values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return playerMutationService.MutateAsync(playerId, player =>
        {
            var current = player.Status;
            player.UpdateStatus(new Status(
                maxHp: values.MaxHp,
                maxMp: values.MaxMp,
                strength: values.Strength,
                defense: values.Defense,
                intelligence: values.Intelligence,
                luck: values.Luck,
                speed: values.Speed,
                accuracy: current.Accuracy,
                evasion: current.Evasion,
                criticalChance: current.CriticalChance,
                damageReduction: current.DamageReduction));
            return Task.FromResult(player);
        });
    }

    /// <summary>
    /// スキルを習得させる。
    /// slotIndex を省略すると空きスロットに入れる（空きが無ければ最も古いスキルを外す。通常の習得と同じ）。
    /// slotIndex を指定するとそのスロットに置く。習得済みのスキルなら元のスロットから移す。
    /// </summary>
    public async Task<Player> LearnMoveAsync(PlayerId playerId, int moveId, int? slotIndex)
    {
        if (slotIndex is < 0 or >= MoveSet.MaxSlots)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), $"slotIndex は0以上{MoveSet.MaxSlots - 1}以下である必要があります。");
        }

        var move = await moveRepository.GetMoveAsync(new MoveId(moveId))
            ?? throw new ArgumentException($"スキル {moveId} は存在しません。", nameof(moveId));

        return await playerMutationService.MutateAsync(playerId, player =>
        {
            if (slotIndex is null)
            {
                player.MoveSet.AddLearnedMove(move.Id);
                return Task.FromResult(player);
            }

            var slots = player.MoveSet.Slots
                .Select(x => x?.Id == move.Id.Id ? null : x)
                .ToArray();
            slots[slotIndex.Value] = move.Id;
            player.UpdateMoveSet(new MoveSet(slots));
            return Task.FromResult(player);
        });
    }

    /// <summary>
    /// 指定したレベルまで上げる。通常のレベルアップと同じく、1 レベルずつ必要経験値を足して
    /// LevelUp を実行するので、ステータスの成長・スキルの習得・ジョブのマスターも通常どおり起きる。
    /// レベルを下げることはできない。
    /// </summary>
    public Task<DebugLevelUpResult> LevelUpToAsync(PlayerId playerId, int targetLevel)
    {
        if (targetLevel is < 1 or > PlayerConstants.MaxLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(targetLevel), $"レベルは1以上{PlayerConstants.MaxLevel}以下である必要があります。");
        }

        return playerMutationService.MutateAsync(playerId, player =>
        {
            if (targetLevel < player.Level)
            {
                throw new ArgumentOutOfRangeException(nameof(targetLevel), $"レベルは下げられません（現在 {player.Level}）。");
            }

            var jobProfile = jobProfileRepository.GetByJob(player.Job);
            var learningRule = jobMoveLearningRuleRepository.GetByJob(player.Job);
            var learnedMoveIds = new List<int>();
            var hasMasteredCurrentJob = false;

            while (player.Level < targetLevel)
            {
                player.GainExp(Math.Max(0, player.RequiredExpForNextLevel() - player.Exp));
                var result = player.LevelUp(jobProfile, learningRule);
                learnedMoveIds.AddRange(result.NewlyLearnedMoveIds.Select(x => x.Id));
                hasMasteredCurrentJob |= result.HasMasteredCurrentJob;
            }

            return Task.FromResult(new DebugLevelUpResult(player.Level, learnedMoveIds, hasMasteredCurrentJob));
        });
    }

    /// <summary>指定したスロットを空にする。</summary>
    public Task<Player> ClearMoveSlotAsync(PlayerId playerId, int slotIndex)
    {
        return playerMutationService.MutateAsync(playerId, player =>
        {
            player.MoveSet.ClearSlot(slotIndex);
            return Task.FromResult(player);
        });
    }
}

public sealed record DebugStatusValues(
    int MaxHp,
    int MaxMp,
    int Strength,
    int Defense,
    int Intelligence,
    int Luck,
    int Speed);

public sealed record DebugLevelUpResult(
    int Level,
    IReadOnlyList<int> NewlyLearnedMoveIds,
    bool HasMasteredCurrentJob);
