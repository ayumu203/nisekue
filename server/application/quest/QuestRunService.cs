using server.application.battle;
using server.domain.battle;
using server.domain.move;
using server.domain.quest;

namespace server.application.quest;

public class QuestRunService(
    IQuestRunRepository questRunRepository,
    IQuestStageRepository questStageRepository,
    IMoveRepository moveRepository,
    BattleService battleService,
    QuestBattleFactory questBattleFactory)
{
    private static readonly TimeSpan TurnDeadline = TimeSpan.FromSeconds(60);

    public async Task<QuestRun> SubmitCommandAsync(QuestRunId runId, QuestParticipantId participantId, QuestSubmittedCommand command)
    {
        var run = await questRunRepository.GetAsync(runId)
            ?? throw new KeyNotFoundException("クエスト進行情報が見つかりません。");

        run.SubmitCommand(participantId, command, DateTimeOffset.UtcNow);
        await questRunRepository.SaveAsync(run);
        return run;
    }

    public async Task<QuestRun> ResolveTimeoutAsync(QuestRunId runId, DateTimeOffset now)
    {
        var run = await questRunRepository.GetAsync(runId)
            ?? throw new KeyNotFoundException("クエスト進行情報が見つかりません。");

        run.SwitchToAutoActionForTimeout(now);
        await questRunRepository.SaveAsync(run);
        return run;
    }

    public async Task<QuestRunResolutionSummary> ResolveTurnAsync(QuestRunId runId)
    {
        var run = await questRunRepository.GetAsync(runId)
            ?? throw new KeyNotFoundException("クエスト進行情報が見つかりません。");
        var stage = await questStageRepository.GetAsync(run.StageId)
            ?? throw new KeyNotFoundException("ステージ定義が見つかりません。");

        var fieldContext = questBattleFactory.CreateBattleFieldContext(run);
        var actors = questBattleFactory.CreateActorInputs(run);
        var (actions, moves) = await questBattleFactory.CreateTurnInputsAsync(run, moveRepository);
        var resolution = battleService.ResolveTurn(new BattleTurnRequest(actors, actions, moves, fieldContext));

        var finalFloorNo = stage.Floors.Max(x => x.FloorNo);
        var summary = run.ApplyBattleResolution(
            resolution,
            questBattleFactory.CreatePartyActorMap(run),
            questBattleFactory.CreateEnemyActorMap(run),
            finalFloorNo,
            DateTimeOffset.UtcNow.Add(TurnDeadline));

        await questRunRepository.SaveAsync(run);
        return summary;
    }

    public async Task<QuestRun> AddChatMessageAsync(QuestRunId runId, QuestChatMessage message)
    {
        var run = await questRunRepository.GetAsync(runId)
            ?? throw new KeyNotFoundException("クエスト進行情報が見つかりません。");

        run.AddChatMessage(message);
        await questRunRepository.SaveAsync(run);
        return run;
    }
}
