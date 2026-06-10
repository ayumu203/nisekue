using FluentAssertions;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.quest;
using Xunit;

namespace server.tests.quest;

public class QuestEnemyStateTests
{
    [Fact]
    public void MarkCaptured_SetsCapturedAndDead()
    {
        var enemy = CreateEnemy();

        enemy.MarkCaptured();

        enemy.IsCaptured.Should().BeTrue();
        enemy.IsDead.Should().BeTrue();
        enemy.CurrentHp.Should().Be(0);
        enemy.IsAlive.Should().BeFalse();
    }

    [Fact]
    public void MarkCaptured_WhenAlreadyDead_ThrowsInvalidOperationException()
    {
        var enemy = CreateEnemy();
        enemy.MarkDead();

        var act = () => enemy.MarkCaptured();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkDead_DoesNotSetCaptured()
    {
        var enemy = CreateEnemy();

        enemy.MarkDead();

        enemy.IsCaptured.Should().BeFalse();
        enemy.IsDead.Should().BeTrue();
    }

    private static QuestEnemyState CreateEnemy()
    {
        return new QuestEnemyState(
            QuestEnemyInstanceId.New(),
            new QuestEnemyDefinitionId(1),
            new BattlePosition(BattleRow.Front, BattleColumn.Right),
            currentHp: 10,
            currentMp: 0,
            isDead: false);
    }
}
