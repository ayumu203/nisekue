using System.Globalization;
using server.domain.player;

namespace server.infrastructure.player;

public class CsvJobRoadmapRankRepository : IJobRoadmapRankRepository
{
    private readonly IReadOnlyDictionary<Job, int> rankByJob;
    private readonly IReadOnlyList<Job> jobs;

    public CsvJobRoadmapRankRepository()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "resources", "player", "job_roadmap_ranks.csv");
        var (ranks, jobList) = LoadRanks(csvPath);
        rankByJob = ranks;
        jobs = jobList;
    }

    public int? GetRank(Job job)
    {
        return rankByJob.TryGetValue(job, out var rank) ? rank : null;
    }

    public IReadOnlyList<Job> GetJobs()
    {
        return jobs;
    }

    private static (IReadOnlyDictionary<Job, int>, IReadOnlyList<Job>) LoadRanks(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new InvalidOperationException($"CSVファイルが見つかりません: {csvPath}");
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            return (new Dictionary<Job, int>(), []);
        }

        var dict = new Dictionary<Job, int>();
        var list = new List<Job>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = line.Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length != 2)
            {
                throw new InvalidOperationException($"job_roadmap_ranks.csv の形式が不正です。行: {i + 1}");
            }

            if (!Enum.TryParse<Job>(columns[0], false, out var job))
            {
                throw new InvalidOperationException($"job_roadmap_ranks.csv の job_code が不正です。value: {columns[0]}, 行: {i + 1}");
            }

            if (!int.TryParse(columns[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rank))
            {
                throw new InvalidOperationException($"job_roadmap_ranks.csv の rank が不正です。value: {columns[1]}, 行: {i + 1}");
            }

            if (dict.ContainsKey(job))
            {
                throw new InvalidOperationException($"job_roadmap_ranks.csv の job_code が重複しています。job_code: {columns[0]}, 行: {i + 1}");
            }

            dict[job] = rank;
            list.Add(job);
        }

        return (dict, list);
    }
}
