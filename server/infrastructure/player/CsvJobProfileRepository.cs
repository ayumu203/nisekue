using System.Globalization;
using server.domain.player;

namespace server.infrastructure.player;

public class CsvJobProfileRepository : IJobProfileRepository
{
    private readonly IReadOnlyDictionary<Job, JobProfile> profileByJob;

    public CsvJobProfileRepository()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "resources", "player", "job_levelup_growths.csv");
        profileByJob = LoadProfiles(csvPath);
    }

    public JobProfile GetByJob(Job job)
    {
        if (profileByJob.TryGetValue(job, out var profile))
        {
            return profile;
        }

        throw new InvalidOperationException($"ジョブ {job} のプロファイルが定義されていません。");
    }

    public IReadOnlyList<JobProfile> GetAll()
    {
        return profileByJob.Values
            .OrderBy(profile => (int)profile.Job)
            .ToArray();
    }

    private static IReadOnlyDictionary<Job, JobProfile> LoadProfiles(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new InvalidOperationException($"CSVファイルが見つかりません: {csvPath}");
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException($"CSVファイルにジョブデータがありません: {csvPath}");
        }

        var header = lines[0].Split(',', StringSplitOptions.TrimEntries);
        var requiredMasterJobIndexes = header
            .Select((name, index) => new { name, index })
            .Where(x => x.name.StartsWith("required_master_job_", StringComparison.Ordinal))
            .Select(x => x.index)
            .OrderBy(x => x)
            .ToArray();

        var map = new Dictionary<Job, JobProfile>();
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
                throw new InvalidOperationException($"CSV形式が不正です。行: {i + 1}");
            }

            var job = ParseJob(columns[GetRequiredColumnIndex(header, "job_code")], i + 1);
            if (map.ContainsKey(job))
            {
                throw new InvalidOperationException($"CSV内でジョブコードが重複しています。job_code: {job}, 行: {i + 1}");
            }

            var requiredMasterJobs = requiredMasterJobIndexes
                .Select(index => columns[index])
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => ParseJob(value, i + 1))
                .ToArray();

            map.Add(job, new JobProfile(
                job: job,
                description: columns[GetRequiredColumnIndex(header, "description")],
                masterLevel: ParseInt(columns[GetRequiredColumnIndex(header, "master_level")], "master_level", i + 1),
                requiredMasterJobs: requiredMasterJobs,
                growthValue: new GrowthValue(
                    MaxHp: ParseInt(columns[GetRequiredColumnIndex(header, "max_hp")], "max_hp", i + 1),
                    MaxMp: ParseInt(columns[GetRequiredColumnIndex(header, "max_mp")], "max_mp", i + 1),
                    Strength: ParseInt(columns[GetRequiredColumnIndex(header, "strength")], "strength", i + 1),
                    Defense: ParseInt(columns[GetRequiredColumnIndex(header, "defense")], "defense", i + 1),
                    Intelligence: ParseInt(columns[GetRequiredColumnIndex(header, "intelligence")], "intelligence", i + 1),
                    Luck: ParseInt(columns[GetRequiredColumnIndex(header, "luck")], "luck", i + 1),
                    Speed: ParseInt(columns[GetRequiredColumnIndex(header, "speed")], "speed", i + 1))));
        }

        if (map.Count == 0)
        {
            throw new InvalidOperationException($"CSVファイルに有効なジョブデータがありません: {csvPath}");
        }

        return map;
    }

    private static int GetRequiredColumnIndex(IReadOnlyList<string> header, string columnName)
    {
        for (var i = 0; i < header.Count; i++)
        {
            if (string.Equals(header[i], columnName, StringComparison.Ordinal))
            {
                return i;
            }
        }

        throw new InvalidOperationException($"CSVヘッダーに必須列がありません。column: {columnName}");
    }

    private static Job ParseJob(string value, int lineNumber)
    {
        if (!Enum.TryParse<Job>(value, ignoreCase: false, out var job))
        {
            throw new InvalidOperationException($"CSVのjob_codeが不正です。value: {value}, 行: {lineNumber}");
        }

        return job;
    }

    private static int ParseInt(string value, string columnName, int lineNumber)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException($"CSVの数値変換に失敗しました。column: {columnName}, 行: {lineNumber}");
        }

        return parsed;
    }
}
