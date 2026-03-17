using server.domain.move;
using server.domain.player;

namespace server.infrastructure.move;

public class CsvJobMoveLearningRuleRepository : IJobMoveLearningRuleRepository
{
    private readonly IReadOnlyDictionary<Job, JobMoveLearningRule> ruleByJob;

    public CsvJobMoveLearningRuleRepository()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "resources", "job_moves.csv");
        ruleByJob = LoadRules(csvPath);
    }

    public JobMoveLearningRule GetByJob(Job job)
    {
        if (ruleByJob.TryGetValue(job, out var rule))
        {
            return rule;
        }

        throw new InvalidOperationException($"ジョブ {job} の技習得ルールが定義されていません。");
    }

    private static IReadOnlyDictionary<Job, JobMoveLearningRule> LoadRules(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new InvalidOperationException($"CSVファイルが見つかりません: {csvPath}");
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException($"CSVファイルにジョブ技データがありません: {csvPath}");
        }

        var header = lines[0].Split(',', StringSplitOptions.TrimEntries);
        var moveIndexes = header
            .Select((name, index) => new { name, index })
            .Where(x => x.name.StartsWith("move_", StringComparison.Ordinal))
            .Select(x => new
            {
                x.index,
                order = ParseMoveOrder(x.name)
            })
            .OrderBy(x => x.order)
            .Select(x => x.index)
            .ToArray();

        if (moveIndexes.Length == 0)
        {
            throw new InvalidOperationException("job_moves.csv に move_ 列がありません。");
        }

        var jobCodeIndex = Array.FindIndex(header, x => string.Equals(x, "job_code", StringComparison.Ordinal));
        if (jobCodeIndex < 0)
        {
            throw new InvalidOperationException("job_moves.csv に job_code 列がありません。");
        }

        var map = new Dictionary<Job, JobMoveLearningRule>();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = line.Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length != header.Length)
            {
                throw new InvalidOperationException($"job_moves.csv の形式が不正です。行: {i + 1}");
            }

            if (!Enum.TryParse<Job>(columns[jobCodeIndex], ignoreCase: false, out var job))
            {
                throw new InvalidOperationException($"job_moves.csv の job_code が不正です。行: {i + 1}");
            }

            if (map.ContainsKey(job))
            {
                throw new InvalidOperationException($"job_moves.csv で job_code が重複しています。job_code={job}, 行: {i + 1}");
            }

            var moveIds = moveIndexes
                .Select(index => columns[index])
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select((value, order) =>
                {
                    if (!int.TryParse(value, out var moveId) || moveId < 1)
                    {
                        throw new InvalidOperationException($"job_moves.csv の move_{order + 1} が不正です。行: {i + 1}");
                    }

                    return new MoveId(moveId);
                })
                .ToArray();

            map.Add(job, new JobMoveLearningRule(job, moveIds));
        }

        if (map.Count == 0)
        {
            throw new InvalidOperationException($"CSVファイルに有効なジョブ技データがありません: {csvPath}");
        }

        return map;
    }

    private static int ParseMoveOrder(string columnName)
    {
        var suffix = columnName["move_".Length..];
        if (!int.TryParse(suffix, out var order) || order < 1)
        {
            throw new InvalidOperationException($"job_moves.csv の列名が不正です。column: {columnName}");
        }

        return order;
    }
}
