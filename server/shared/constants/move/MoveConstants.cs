namespace server.shared.constants.move;

public static class MoveConstants
{
    public static class Constraints
    {
        public const int NameMaxLength = 20;
        public const int DescriptionMaxLength = 120;
        public const int MinId = 1;
        public const int MinSequence = 1;
        public const int MinHitCount = 1;
        public const int MinBuffTurns = 1;
        public const int MinMpCost = 0;
        public const int MinFixedValue = 0;
        public const decimal MinRate = 0m;
        public const decimal MaxRate = 1m;
    }
}
