using FluentAssertions;
using server.application.player;
using server.domain.move;
using server.domain.player;
using Xunit;

namespace server.tests;

public class PlayerJobServiceTests
{
    [Fact]
    public async Task ChangeJobAsync_WhenNextJobIsRanger_ChangesJobSuccessfully()
    {
        var player = new Player(
            new PlayerId(Guid.NewGuid()),
            "Tester",
            level: 5,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(30, 10, 8, 6, 4, 3, 5),
            job: Job.Apprentice);
        var playerRepository = new FakePlayerRepository(player);
        var service = new PlayerJobService(
            playerRepository,
            new FakeJobProfileRepository(),
            new FakeJobMoveLearningRuleRepository(),
            new FakeMoveRepository());

        var result = await service.ChangeJobAsync(player.Id, Job.Ranger);

        result.Player.Job.Should().Be(Job.Ranger);
        playerRepository.SaveCount.Should().Be(1);
    }

    private sealed class FakePlayerRepository(Player player) : IPlayerRepository
    {
        public int SaveCount { get; private set; }

        public Task<Player?> GetPlayerAsync(PlayerId id)
        {
            return Task.FromResult<Player?>(player.Id == id ? player : null);
        }

        public Task<IReadOnlyList<Player>> GetAllAsync()
        {
            return Task.FromResult<IReadOnlyList<Player>>([player]);
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
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeJobProfileRepository : IJobProfileRepository
    {
        public JobProfile GetByJob(Job job)
        {
            return new JobProfile(
                job,
                $"{job} profile",
                masterLevel: 5,
                requiredMasterJobs: [],
                growthValue: new GrowthValue(1, 1, 1, 1, 1, 1, 1));
        }

        public IReadOnlyList<JobProfile> GetAll()
        {
            return Enum.GetValues<Job>()
                .Select(GetByJob)
                .ToArray();
        }
    }

    private sealed class FakeJobMoveLearningRuleRepository : IJobMoveLearningRuleRepository
    {
        public JobMoveLearningRule GetByJob(Job job)
        {
            return new JobMoveLearningRule(job, []);
        }
    }

    private sealed class FakeMoveRepository : IMoveRepository
    {
        public Task<Move?> GetMoveAsync(MoveId moveId)
        {
            return Task.FromResult<Move?>(null);
        }

        public Task<IReadOnlyList<Move>> GetAllMovesAsync()
        {
            return Task.FromResult<IReadOnlyList<Move>>([]);
        }
    }
}
