namespace server.domain.move.enums;

// The JSON serializer in Quest saves ailment types as their integral values. Adjustments to the
// existing values break deserialization of saved quest effects, so keep current assignments intact
// and append any new values after the existing set.
public enum AilmentType
{
    Paralysis = 1,
    Poison = 2,
    Sleep = 3,
    Burn = 4,
    Taunt = 5,
    PoisonTrap = 6,
    DamageTrap = 7,
    InstantDeath = 8,
    Regeneration = 9,
    CoverAll = 10
}
