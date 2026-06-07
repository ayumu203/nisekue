namespace server.domain.player;

public static class JobHierarchy
{
    private static readonly IReadOnlyDictionary<Job, IReadOnlySet<Job>> AncestorsByJob = BuildAncestors();

    public static IReadOnlySet<Job> GetAncestors(Job job)
    {
        return AncestorsByJob.TryGetValue(job, out var ancestors) ? ancestors : new HashSet<Job>();
    }

    private static IReadOnlyDictionary<Job, IReadOnlySet<Job>> BuildAncestors()
    {
        var directParents = new Dictionary<Job, IReadOnlyList<Job>>
        {
            [Job.Apprentice] = [],
            [Job.Warrior] = [],
            [Job.Guardian] = [],
            [Job.Mage] = [],
            [Job.Priest] = [],
            [Job.Ranger] = [],
            [Job.OniWarrior] = [Job.Warrior],
            [Job.SwordMaster] = [Job.Warrior],
            [Job.Trickster] = [Job.Guardian],
            [Job.Crusader] = [Job.Guardian],
            [Job.FireMage] = [Job.Mage],
            [Job.WaterMage] = [Job.Mage],
            [Job.WindMage] = [Job.Mage],
            [Job.HighPriest] = [Job.Priest],
            [Job.Necromancer] = [Job.Priest],
            [Job.Sniper] = [Job.Ranger],
            [Job.TrapMaster] = [Job.Ranger],
            [Job.GrandWarrior] = [Job.OniWarrior, Job.SwordMaster],
            [Job.GrandGuard] = [Job.SwordMaster, Job.Trickster],
            [Job.GrandCaster] = [Job.FireMage, Job.WaterMage, Job.WindMage],
            [Job.GrandPriest] = [Job.HighPriest, Job.Necromancer],
            [Job.GrandRanger] = [Job.Sniper, Job.TrapMaster],
            [Job.Shogun] = [Job.GrandWarrior, Job.GrandGuard],
            [Job.Archmage] = [Job.GrandCaster, Job.GrandPriest],
            [Job.GreatThief] = [Job.Warrior, Job.WindMage, Job.GrandRanger],
            [Job.Bushin] = [Job.GrandWarrior],
            [Job.Seikaiou] = [Job.GrandCaster],
            [Job.Matouou] = [Job.GreatThief],
        };

        var result = new Dictionary<Job, IReadOnlySet<Job>>();
        foreach (var job in Enum.GetValues<Job>())
        {
            result[job] = ComputeAncestors(job, directParents);
        }

        return result;
    }

    private static IReadOnlySet<Job> ComputeAncestors(Job job, Dictionary<Job, IReadOnlyList<Job>> directParents)
    {
        var ancestors = new HashSet<Job>();
        var queue = new Queue<Job>(directParents[job]);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (ancestors.Add(current))
            {
                foreach (var parent in directParents[current])
                {
                    queue.Enqueue(parent);
                }
            }
        }

        return ancestors;
    }
}
