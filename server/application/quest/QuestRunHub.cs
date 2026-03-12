using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using server.domain.quest;

namespace server.application.quest;

[Authorize]
public class QuestRunHub(
    IQuestRunRepository questRunRepository,
    QuestResponseMapper responseMapper) : Hub
{
    public async Task SubscribeRun(Guid runId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, runId.ToString());

        var run = await questRunRepository.GetAsync(new QuestRunId(runId));
        if (run is null)
        {
            await Clients.Caller.SendAsync("QuestRunError", new
            {
                code = "run_not_found",
                message = "クエスト進行情報が見つかりません。"
            });
            return;
        }

        var payload = await responseMapper.MapQuestRunDetailAsync(run);
        await Clients.Caller.SendAsync("QuestRunSnapshot", payload);
    }

    public Task UnsubscribeRun(Guid runId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, runId.ToString());
    }
}
