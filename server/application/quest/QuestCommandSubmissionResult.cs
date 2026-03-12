using server.domain.quest;

namespace server.application.quest;

public sealed record QuestCommandSubmissionResult(QuestRun Run, bool ResolvedInThisRequest);
