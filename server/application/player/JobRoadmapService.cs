using server.domain.player;
using server.domain.quest;

namespace server.application.player;

public record JobRoadmapListEntry(
    int JobId,
    string JobCode,
    string JobName,
    int Rank,
    bool IsUnlocked,
    bool CanUnlock,
    int GoldCostToUnlock);

public record QuestStageReference(int Id, string Name);

public record JobRoadmapRequirement(int Id, string Code, string Name);

public record JobRoadmapNode(
    string Type,
    int? JobId,
    string? JobCode,
    string? JobName,
    int? Rank,
    int? ItemId,
    string? ItemName,
    bool IsUnlocked,
    int? GoldCostToUnlock,
    IReadOnlyList<QuestStageReference>? Stages,
    IReadOnlyList<JobRoadmapRequirement>? RequiredMasterJobs,
    IReadOnlyList<JobRoadmapNode> Requirements);

public record UnlockJobRoadmapResult(
    int JobId,
    string JobName,
    int PaidGold,
    int RemainingGold);

public class JobRoadmapService(
    IPlayerRepository playerRepository,
    IJobProfileRepository jobProfileRepository,
    IJobRoadmapRankRepository roadmapRankRepository,
    IItemRepository itemRepository,
    IQuestStageRepository questStageRepository,
    IPlayerMutationService? playerMutationService = null)
{
    private IReadOnlyDictionary<Job, IReadOnlyList<Item>>? cachedItemsByTargetJob;

    public async Task<IReadOnlyList<JobRoadmapListEntry>> GetListAsync(PlayerId playerId)
    {
        var player = await playerRepository.GetPlayerAsync(playerId)
            ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");

        var allJobs = roadmapRankRepository.GetJobs();
        var entries = new List<JobRoadmapListEntry>(allJobs.Count);

        foreach (var job in allJobs)
        {
            var rank = roadmapRankRepository.GetRank(job) ?? 0;
            var isUnlocked = player.IsRoadmapUnlocked(job);
            var canUnlock = !isUnlocked && await CanUnlockRoadmapAsync(player, job);
            var goldCost = GoldCostForRank(rank);

            entries.Add(new JobRoadmapListEntry(
                (int)job,
                job.ToString(),
                JobDisplayNames.GetDisplayName(job),
                rank,
                isUnlocked,
                canUnlock,
                goldCost));
        }

        return entries;
    }

    public async Task<JobRoadmapNode> GetRoadmapAsync(PlayerId playerId, Job targetJob)
    {
        var player = await playerRepository.GetPlayerAsync(playerId)
            ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");

        var visited = new HashSet<Job>();
        return BuildJobNode(player, targetJob, visited);
    }

    public async Task<UnlockJobRoadmapResult> UnlockAsync(PlayerId playerId, Job targetJob)
    {
        var rank = roadmapRankRepository.GetRank(targetJob);
        if (rank is null)
        {
            throw new InvalidOperationException("指定されたジョブのロードマップ情報が存在しません。");
        }

        if (playerMutationService is null)
        {
            var player = await playerRepository.GetPlayerAsync(playerId)
                ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");

            if (player.IsRoadmapUnlocked(targetJob))
            {
                return new UnlockJobRoadmapResult(
                    (int)targetJob,
                    JobDisplayNames.GetDisplayName(targetJob),
                    0,
                    player.Gold);
            }

            if (!await CanUnlockRoadmapAsync(player, targetJob))
            {
                throw new InvalidOperationException("前提となるジョブのロードマップが解放されていません。");
            }

            var goldCost = GoldCostForRank(rank.Value);
            player.SpendGold(goldCost);
            player.UnlockRoadmap(targetJob);
            await playerRepository.SaveAsync(player);

            return new UnlockJobRoadmapResult(
                (int)targetJob,
                JobDisplayNames.GetDisplayName(targetJob),
                goldCost,
                player.Gold);
        }

        return await playerMutationService.MutateAsync(playerId, async player =>
        {
            if (player.IsRoadmapUnlocked(targetJob))
            {
                return new UnlockJobRoadmapResult(
                    (int)targetJob,
                    JobDisplayNames.GetDisplayName(targetJob),
                    0,
                    player.Gold);
            }

            if (!await CanUnlockRoadmapAsync(player, targetJob))
            {
                throw new InvalidOperationException("前提となるジョブのロードマップが解放されていません。");
            }

            var goldCost = GoldCostForRank(rank.Value);
            player.SpendGold(goldCost);
            player.UnlockRoadmap(targetJob);

            return new UnlockJobRoadmapResult(
                (int)targetJob,
                JobDisplayNames.GetDisplayName(targetJob),
                goldCost,
                player.Gold);
        });
    }

    private async Task<bool> CanUnlockRoadmapAsync(Player player, Job targetJob)
    {
        var profile = jobProfileRepository.GetByJob(targetJob);

        foreach (var prerequisiteJob in profile.RequiredMasterJobs)
        {
            if (!player.IsRoadmapUnlocked(prerequisiteJob))
            {
                return false;
            }
        }

        return true;
    }

    private static int GoldCostForRank(int rank) => rank switch
    {
        0 => 1,
        1 => 1000,
        2 => 10000,
        3 => 50000,
        4 => 100000,
        5 => 500000,
        _ => throw new ArgumentOutOfRangeException(nameof(rank)),
    };

    private JobRoadmapNode BuildJobNode(Player player, Job job, HashSet<Job> visited)
    {
        if (!visited.Add(job))
        {
            throw new InvalidOperationException($"ロードマップデータに循環参照が検出されました。job: {job}");
        }

        var rank = roadmapRankRepository.GetRank(job);
        var isUnlocked = player.IsRoadmapUnlocked(job);
        var goldCost = rank is not null ? GoldCostForRank(rank.Value) : (int?)null;
        var requirements = new List<JobRoadmapNode>();

        if (isUnlocked)
        {
            var profile = jobProfileRepository.GetByJob(job);
            foreach (var prerequisiteJob in profile.RequiredMasterJobs)
            {
                requirements.Add(BuildJobNode(player, prerequisiteJob, visited));
            }

            var items = GetChangeJobItemsFor(job);
            foreach (var item in items)
            {
                requirements.Add(BuildItemNode(item));
            }
        }

        visited.Remove(job);

        return new JobRoadmapNode(
            Type: "job",
            JobId: (int)job,
            JobCode: job.ToString(),
            JobName: JobDisplayNames.GetDisplayName(job),
            Rank: rank,
            ItemId: null,
            ItemName: null,
            IsUnlocked: isUnlocked,
            GoldCostToUnlock: goldCost,
            Stages: null,
            RequiredMasterJobs: null,
            Requirements: requirements);
    }

    private JobRoadmapNode BuildItemNode(Item item)
    {
        var stageRefs = GetStagesForItem(item.Id);

        var requiredMasterJobRefs = item.RequiredMasterJobs
            .Select(job => new JobRoadmapRequirement(
                (int)job,
                job.ToString(),
                JobDisplayNames.GetDisplayName(job)))
            .ToArray();

        return new JobRoadmapNode(
            Type: "item",
            JobId: null,
            JobCode: null,
            JobName: null,
            Rank: null,
            ItemId: item.Id.Value,
            ItemName: item.Name,
            IsUnlocked: false,
            GoldCostToUnlock: null,
            Stages: stageRefs.Any() ? stageRefs : null,
            RequiredMasterJobs: requiredMasterJobRefs.Any() ? requiredMasterJobRefs : null,
            Requirements: []);
    }

    private IReadOnlyList<Item> GetChangeJobItemsFor(Job targetJob)
    {
        if (cachedItemsByTargetJob is null)
        {
            var allItems = itemRepository.GetAllAsync().GetAwaiter().GetResult();
            cachedItemsByTargetJob = allItems
                .Where(item => item.EffectType == ItemEffectType.ChangeJob && item.ChangeJobTo is not null)
                .GroupBy(item => item.ChangeJobTo!.Value)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<Item>)g.ToArray());
        }

        return cachedItemsByTargetJob.TryGetValue(targetJob, out var items) ? items : [];
    }

    private List<QuestStageReference> GetStagesForItem(ItemId itemId)
    {
        var allStages = questStageRepository.GetAllAsync().GetAwaiter().GetResult();
        var stageRefs = new List<QuestStageReference>();

        foreach (var stage in allStages)
        {
            foreach (var reward in stage.ItemRewards)
            {
                if (reward.ItemId?.Value == itemId.Value)
                {
                    stageRefs.Add(new QuestStageReference(stage.Id.Value, stage.Name));
                    break;
                }
            }
        }

        return stageRefs;
    }
}
