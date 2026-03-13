using System.Globalization;
using server.domain.battle;
using server.domain.battle.enums;

namespace server.infrastructure.quest;

internal static class CsvQuestParser
{
    public static string[] ReadDataLines(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new InvalidOperationException($"CSVファイルが見つかりません: {csvPath}");
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException($"CSVファイルにデータがありません: {csvPath}");
        }

        return lines;
    }

    public static string[] SplitColumns(string line)
    {
        return line.Split(',', StringSplitOptions.TrimEntries);
    }

    public static int ParseInt(string value, string columnName, int lineNumber)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException($"CSVの数値変換に失敗しました。column: {columnName}, 行: {lineNumber}");
        }

        return parsed;
    }

    public static decimal ParseDecimal(string value, string columnName, int lineNumber)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException($"CSVの小数変換に失敗しました。column: {columnName}, 行: {lineNumber}");
        }

        return parsed;
    }

    public static bool ParseBool(string value, string columnName, int lineNumber)
    {
        if (!bool.TryParse(value, out var parsed))
        {
            throw new InvalidOperationException($"CSVの bool 変換に失敗しました。column: {columnName}, 行: {lineNumber}");
        }

        return parsed;
    }

    public static TEnum ParseEnum<TEnum>(string value, string columnName, int lineNumber) where TEnum : struct
    {
        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
        {
            throw new InvalidOperationException($"CSVの enum 変換に失敗しました。column: {columnName}, 行: {lineNumber}, value: {value}");
        }

        return parsed;
    }

    public static int[] ParseIntList(string value, string columnName, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(x => ParseInt(x, columnName, lineNumber))
            .ToArray();
    }

    public static BattlePosition ParseBattlePosition(string rowValue, string columnValue, int lineNumber)
    {
        var row = ParseEnum<BattleRow>(rowValue, "battle_row", lineNumber);
        var column = ParseEnum<BattleColumn>(columnValue, "battle_column", lineNumber);
        return new BattlePosition(row, column);
    }
}
