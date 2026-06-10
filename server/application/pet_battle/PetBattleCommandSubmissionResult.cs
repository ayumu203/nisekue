using server.domain.pet_battle;

namespace server.application.pet_battle;

public sealed record PetBattleCommandSubmissionResult(PetBattleRun Run, bool ResolvedInThisRequest);
