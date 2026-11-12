using server.application.player;
using server.domain.player;

namespace server.application.debug;

/// <summary>デバッグ用テンプレートの種類。</summary>
public enum DebugTemplateKind
{
    /// <summary>作成直後（見習い Lv1）。</summary>
    Initial,

    /// <summary>見習い → 一次職をマスターした状態。</summary>
    FirstTierMastered,
}

/// <summary>デバッグ用テンプレート。Job は FirstTierMastered のときの対象ジョブ。</summary>
public sealed record DebugTemplate(string Id, DebugTemplateKind Kind, Job? Job);

public sealed record DebugApplyTemplateResult(int Level, Job Job, IReadOnlyCollection<Job> MasteredJobs);

/// <summary>
/// デバッグ用テンプレート（戻ってくる地点）を取り込む。
/// テンプレートは固定の数値ではなく手順として持ち、取り込むたびにその時点のマスタデータで
/// 通常のレベルアップ・転職を実行する（スプレッドシートで成長値を変えても追従する）。
/// </summary>
public class DebugTemplateService(
    IPlayerMutationService playerMutationService,
    IJobProfileRepository jobProfileRepository,
    IJobMoveLearningRuleRepository jobMoveLearningRuleRepository)
{
    public const string InitialTemplateId = "initial";
    private const string FirstTierMasteredPrefix = "master-";

    /// <summary>一次職への転職に必要なプレイヤーレベル（Player.CanChangeJob と同じ条件）。</summary>
    private const int FirstTierRequiredLevel = 5;

    /// <summary>テンプレートの一覧。一次職はマスタデータの「前提ジョブなし・見習い以外」のジョブ。</summary>
    public IReadOnlyList<DebugTemplate> GetTemplates()
    {
        var templates = new List<DebugTemplate> { new(InitialTemplateId, DebugTemplateKind.Initial, null) };
        templates.AddRange(jobProfileRepository.GetAll()
            .Where(x => x.Job != Job.Apprentice && x.RequiredMasterJobs.Count == 0)
            .OrderBy(x => (int)x.Job)
            .Select(x => new DebugTemplate($"{FirstTierMasteredPrefix}{x.Job}", DebugTemplateKind.FirstTierMastered, x.Job)));
        return templates;
    }

    public Task<DebugApplyTemplateResult> ApplyAsync(PlayerId playerId, string templateId)
    {
        var template = GetTemplates().FirstOrDefault(x => x.Id == templateId)
            ?? throw new ArgumentException($"テンプレート {templateId} は存在しません。", nameof(templateId));

        return playerMutationService.MutateAsync(playerId, player =>
        {
            player.ResetProgress(PlayerInitialState.CreateStatus(), PlayerInitialState.CreateMoveSet());

            if (template is { Kind: DebugTemplateKind.FirstTierMastered, Job: { } job })
            {
                LevelUpUntil(player, p => p.MasteredJobs.Contains(Job.Apprentice) && p.Level >= FirstTierRequiredLevel);
                player.ChangeJob(job, jobProfileRepository.GetByJob(job), jobMoveLearningRuleRepository.GetByJob(job));
                LevelUpUntil(player, p => p.MasteredJobs.Contains(job));
            }

            return Task.FromResult(new DebugApplyTemplateResult(player.Level, player.Job, player.MasteredJobs.ToArray()));
        });
    }

    /// <summary>条件を満たすまで、通常のレベルアップを 1 レベルずつ実行する。</summary>
    private void LevelUpUntil(Player player, Func<Player, bool> isDone)
    {
        var jobProfile = jobProfileRepository.GetByJob(player.Job);
        var learningRule = jobMoveLearningRuleRepository.GetByJob(player.Job);

        while (!isDone(player))
        {
            if (player.IsMaxLevel)
            {
                throw new InvalidOperationException(
                    $"最大レベルに達しても条件を満たせませんでした（{player.Job}）。マスタデータのマスターレベルを確認してください。");
            }

            player.GainExp(Math.Max(0, player.RequiredExpForNextLevel() - player.Exp));
            player.LevelUp(jobProfile, learningRule);
        }
    }
}
