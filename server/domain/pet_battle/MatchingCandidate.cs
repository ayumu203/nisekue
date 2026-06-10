using server.domain.player;

namespace server.domain.pet_battle;

public record MatchingCandidate(PlayerId PlayerId, int Rating, int PetCount);
