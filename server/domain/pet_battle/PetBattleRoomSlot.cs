using server.domain.battle;
using server.domain.battle.enums;
using server.domain.pet;

namespace server.domain.pet_battle;

public record PetBattleRoomSlot(PlayerPetId PetId, BattleRow Row, BattleColumn Column);
