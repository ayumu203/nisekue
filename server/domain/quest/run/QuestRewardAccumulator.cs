namespace server.domain.quest;

public class QuestRewardAccumulator(int exp = 0)
{
    public int Exp { get; private set; } = ValidateNonNegative(exp);

    public void AddExp(int value)
    {
        Exp += ValidateNonNegative(value);
    }

    private static int ValidateNonNegative(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "0以上である必要があります。");
        }

        return value;
    }
}
