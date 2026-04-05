namespace server.domain.move.enums;

public enum AttackRange
{
    Single = 1,
    Column = 2,
    Row = 3,
    [Obsolete("Use Column instead.")]
    AcrossRows = Column,
    [Obsolete("Use Row instead.")]
    AcrossColumns = Row,
    Square = 4,
    All = 5
}
