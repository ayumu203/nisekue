using FluentAssertions;
using server.application.player;
using server.domain.player;
using server.domain.quest;
using Xunit;

namespace server.tests;

public class JobRoadmapServiceTests
{
    [Fact]
    public async Task GetListAsync_WhenPlayerExists_ReturnsAllJobsWithUnlockStates()
    {
        var player = CreatePlayer(level: 1, gold: 1000);
        var playerRepository = new FakePlayerRepository(player);
        var service = new JobRoadmapService(
            playerRepository,
            new FakeJobProfileRepository(),
            new FakeJobRoadmapRankRepository(),
            new FakeItemRepository(),
            new FakeQuestStageRepository(),
            new TestPlayerMutationService(playerRepository));

        var list = await service.GetListAsync(player.Id);

        list.Should().HaveCount(2);
        list[0].JobCode.Should().Be("Apprentice");
        list[0].IsUnlocked.Should().BeTrue();
        list[1].JobCode.Should().Be("Warrior");
        list[1].IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public async Task UnlockAsync_WhenPrerequisiteUnlocked_SucceedsAndSpendsGold()
    {
        var player = CreatePlayer(level: 1, gold: 2000);
        player.UnlockRoadmap(Job.Apprentice);
        var playerRepository = new FakePlayerRepository(player);
        var service = new JobRoadmapService(
            playerRepository,
            new FakeJobProfileRepository(),
            new FakeJobRoadmapRankRepository(),
            new FakeItemRepository(),
            new FakeQuestStageRepository(),
            new TestPlayerMutationService(playerRepository));

        var result = await service.UnlockAsync(player.Id, Job.Warrior);

        result.PaidGold.Should().Be(1000);
        result.RemainingGold.Should().Be(1000);
        player.IsRoadmapUnlocked(Job.Warrior).Should().BeTrue();
    }

    [Fact]
    public async Task UnlockAsync_WhenAlreadyUnlocked_SucceedsWithoutSpendingGold()
    {
        var player = CreatePlayer(level: 1, gold: 1000);
        player.UnlockRoadmap(Job.Apprentice);
        player.UnlockRoadmap(Job.Warrior);
        var playerRepository = new FakePlayerRepository(player);
        var service = new JobRoadmapService(
            playerRepository,
            new FakeJobProfileRepository(),
            new FakeJobRoadmapRankRepository(),
            new FakeItemRepository(),
            new FakeQuestStageRepository(),
            new TestPlayerMutationService(playerRepository));

        var result = await service.UnlockAsync(player.Id, Job.Warrior);

        result.PaidGold.Should().Be(0);
        result.RemainingGold.Should().Be(1000);
    }

    [Fact]
    public async Task UnlockAsync_WhenPrerequisiteNotUnlocked_ThrowsInvalidOperationException()
    {
        var player = CreatePlayer(level: 1, gold: 100000);
        var playerRepository = new FakePlayerRepository(player);
        var service = new JobRoadmapService(
            playerRepository,
            new FakeJobProfileRepository(),
            new FakeJobRoadmapRankRepository(),
            new FakeItemRepository(),
            new FakeQuestStageRepository(),
            new TestPlayerMutationService(playerRepository));

        var act = async () => await service.UnlockAsync(player.Id, Job.OniWarrior);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*前提*");
    }

    [Fact]
    public async Task UnlockAsync_WhenGoldInsufficient_ThrowsInvalidOperationException()
    {
        var player = CreatePlayer(level: 1, gold: 50);
        player.UnlockRoadmap(Job.Apprentice);
        var playerRepository = new FakePlayerRepository(player);
        var service = new JobRoadmapService(
            playerRepository,
            new FakeJobProfileRepository(),
            new FakeJobRoadmapRankRepository(),
            new FakeItemRepository(),
            new FakeQuestStageRepository(),
            new TestPlayerMutationService(playerRepository));

        var act = async () => await service.UnlockAsync(player.Id, Job.Warrior);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*不足*");
    }

    [Fact]
    public async Task GetRoadmapAsync_WhenBasicJob_ReturnsNodeWithEmptyRequirements()
    {
        var player = CreatePlayer(level: 1, gold: 100);
        var playerRepository = new FakePlayerRepository(player);
        var service = new JobRoadmapService(
            playerRepository,
            new FakeJobProfileRepository(),
            new FakeJobRoadmapRankRepository(),
            new FakeItemRepository(),
            new FakeQuestStageRepository(),
            new TestPlayerMutationService(playerRepository));

        var roadmap = await service.GetRoadmapAsync(player.Id, Job.Warrior);

        roadmap.Type.Should().Be("job");
        roadmap.JobCode.Should().Be("Warrior");
        roadmap.Rank.Should().Be(1);
        roadmap.Requirements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoadmapAsync_WhenAdvancedJob_HasPrerequisiteRequirements()
    {
        var player = CreatePlayer(level: 1, gold: 100);
        player.UnlockRoadmap(Job.OniWarrior);
        var playerRepository = new FakePlayerRepository(player);
        var service = new JobRoadmapService(
            playerRepository,
            new FakeJobProfileRepository(),
            new FakeJobRoadmapRankRepository(),
            new FakeItemRepository(),
            new FakeQuestStageRepository(),
            new TestPlayerMutationService(playerRepository));

        var roadmap = await service.GetRoadmapAsync(player.Id, Job.OniWarrior);

        roadmap.Type.Should().Be("job");
        roadmap.Requirements.Should().HaveCount(1);
        roadmap.Requirements[0].JobCode.Should().Be("Warrior");
    }

    [Fact]
    public async Task GetRoadmapAsync_WhenJobHasChangeItem_IncludesItemNode()
    {
        var player = CreatePlayer(level: 1, gold: 100);
        player.UnlockRoadmap(Job.OniWarrior);
        var playerRepository = new FakePlayerRepository(player);
        var items = new List<Item>
        {
            new(
                new ItemId(3052),
                "鬼武者の証",
                "flavor",
                99,
                ItemEffectType.ChangeJob,
                changeJobTo: Job.OniWarrior,
                requiredMasterJobs: new HashSet<Job> { Job.Warrior }),
        };
        var service = new JobRoadmapService(
            playerRepository,
            new FakeJobProfileRepository(),
            new FakeJobRoadmapRankRepository(),
            new FakeItemRepository(items),
            new FakeQuestStageRepository(),
            new TestPlayerMutationService(playerRepository));

        var roadmap = await service.GetRoadmapAsync(player.Id, Job.OniWarrior);

        roadmap.Requirements.Should().Contain(node => node.Type == "item" && node.ItemName == "鬼武者の証");
    }

    private static Player CreatePlayer(int level, int gold)
    {
        return new Player(
            new PlayerId(Guid.NewGuid()),
            "Tester",
            level: level,
            exp: 0,
            jobLevel: 1,
            gold: gold,
            status: new Status(30, 10, 8, 6, 4, 3, 5),
            job: Job.Apprentice);
    }

    private sealed class FakePlayerRepository(Player initialPlayer) : IPlayerRepository
    {
        private Player storedPlayer = initialPlayer;

        public Task<Player?> GetPlayerAsync(PlayerId id)
        {
            return Task.FromResult<Player?>(storedPlayer.Id == id ? storedPlayer : null);
        }

        public Task<Player?> GetPlayerWithinLevelCapAsync(PlayerId id, int maxLevel)
        {
            return Task.FromResult<Player?>(storedPlayer.Id == id && storedPlayer.Level <= maxLevel ? storedPlayer : null);
        }

        public Task<IReadOnlyList<Player>> GetPvpOpponentsAsync(PlayerId excludeId, int maxLevel, int? offset = null, int? limit = null)
        {
            return Task.FromResult<IReadOnlyList<Player>>(Array.Empty<Player>());
        }

        public async Task<(IReadOnlyList<Player> Opponents, int TotalCount)> GetPvpOpponentsPageAsync(
            PlayerId excludeId,
            int maxLevel,
            int? offset = null,
            int? limit = null)
        {
            var opponents = await GetPvpOpponentsAsync(excludeId, maxLevel, offset, limit);
            var totalCount = await CountPvpOpponentsAsync(excludeId, maxLevel);
            return (opponents, totalCount);
        }

        public Task<int> CountPvpOpponentsAsync(PlayerId excludeId, int maxLevel)
        {
            return Task.FromResult(0);
        }

        public Task<IReadOnlyList<Player>> GetAllAsync(int? offset = null, int? limit = null)
        {
            return Task.FromResult<IReadOnlyList<Player>>([storedPlayer]);
        }

        public Task<int> CountAllAsync()
        {
            return Task.FromResult(1);
        }

        public Task<IReadOnlyList<Player>> GetPlayersAsync(IEnumerable<PlayerId> ids)
        {
            var idSet = ids.ToHashSet();
            IReadOnlyList<Player> result = idSet.Contains(storedPlayer.Id) ? [storedPlayer] : [];
            return Task.FromResult(result);
        }

        public async Task<(IReadOnlyList<Player> Players, int TotalCount)> GetPlayersPageExcludingAsync(
            PlayerId excludeId,
            int? offset = null,
            int? limit = null)
        {
            var allPlayers = await GetAllAsync();
            var filtered = allPlayers.Where(x => x.Id != excludeId).ToArray();
            var paged = filtered.AsEnumerable();

            if (offset is > 0)
            {
                paged = paged.Skip(offset.Value);
            }

            if (limit is > 0)
            {
                paged = paged.Take(limit.Value);
            }

            return (paged.ToArray(), filtered.Length);
        }

        public Task<bool> UpdateNameAsync(PlayerId id, string name)
        {
            throw new NotSupportedException();
        }

        public Task<DateTimeOffset?> TryStartTrainingCooldownAsync(PlayerId id, DateTimeOffset nowUtc, TimeSpan cooldown)
        {
            throw new NotSupportedException();
        }

        public Task SaveAsync(Player savedPlayer)
        {
            storedPlayer = savedPlayer;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeJobProfileRepository : IJobProfileRepository
    {
        public JobProfile GetByJob(Job job)
        {
            return job switch
            {
                Job.Apprentice => new JobProfile(
                    job,
                    "description",
                    masterLevel: 5,
                    requiredMasterJobs: [],
                    growthValue: new GrowthValue(4, 2, 2, 2, 2, 1, 1)),
                Job.Warrior => new JobProfile(
                    job,
                    "description",
                    masterLevel: 20,
                    requiredMasterJobs: [],
                    growthValue: new GrowthValue(6, 1, 3, 2, 0, 1, 1)),
                Job.OniWarrior => new JobProfile(
                    job,
                    "description",
                    masterLevel: 30,
                    requiredMasterJobs: [Job.Warrior],
                    growthValue: new GrowthValue(8, 1, 6, 1, 0, 1, 3)),
                _ => new JobProfile(
                    job,
                    "description",
                    masterLevel: 1,
                    requiredMasterJobs: [],
                    growthValue: new GrowthValue(1, 1, 1, 1, 1, 1, 1)),
            };
        }

        public IReadOnlyList<JobProfile> GetAll()
        {
            return
            [
                GetByJob(Job.Apprentice),
                GetByJob(Job.Warrior),
                GetByJob(Job.OniWarrior),
            ];
        }
    }

    private sealed class FakeJobRoadmapRankRepository : IJobRoadmapRankRepository
    {
        public int? GetRank(Job job)
        {
            return job switch
            {
                Job.Apprentice => 0,
                Job.Warrior => 1,
                Job.OniWarrior => 2,
                _ => null,
            };
        }

        public IReadOnlyList<Job> GetJobs()
        {
            return [Job.Apprentice, Job.Warrior];
        }
    }

    private sealed class FakeItemRepository(IReadOnlyList<Item>? items = null) : IItemRepository
    {
        public Task<Item?> GetAsync(ItemId id)
        {
            return Task.FromResult<Item?>(null);
        }

        public Task<IReadOnlyList<Item>> GetAllAsync()
        {
            return Task.FromResult(items ?? (IReadOnlyList<Item>)Array.Empty<Item>());
        }
    }

    private sealed class FakeQuestStageRepository : IQuestStageRepository
    {
        public Task<QuestStageDefinition?> GetAsync(QuestStageId id)
        {
            return Task.FromResult<QuestStageDefinition?>(null);
        }

        public Task<QuestStageDefinition?> GetByStageCodeAsync(string stageCode)
        {
            return Task.FromResult<QuestStageDefinition?>(null);
        }

        public Task<IReadOnlyList<QuestStageDefinition>> GetAllAsync()
        {
            return Task.FromResult<IReadOnlyList<QuestStageDefinition>>(Array.Empty<QuestStageDefinition>());
        }
    }
}
