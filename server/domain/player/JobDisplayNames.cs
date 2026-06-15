namespace server.domain.player;

public static class JobDisplayNames
{
    public static string GetDisplayName(Job job) =>
        job switch
        {
            Job.Apprentice => "見習い",
            Job.Warrior => "戦士",
            Job.Guardian => "盾使い",
            Job.Mage => "魔法使い",
            Job.Priest => "僧侶",
            Job.Ranger => "レンジャー",
            Job.OniWarrior => "鬼武者",
            Job.SwordMaster => "ソードマスター",
            Job.Trickster => "トリックスター",
            Job.Crusader => "クルセイダー",
            Job.FireMage => "火魔法使い",
            Job.WaterMage => "水魔法使い",
            Job.WindMage => "風魔法使い",
            Job.HighPriest => "神官",
            Job.Necromancer => "死霊使い",
            Job.Sniper => "スナイパー",
            Job.TrapMaster => "罠師",
            Job.GrandWarrior => "グランドウォリアー",
            Job.GrandGuard => "グランドガード",
            Job.GrandCaster => "グランドキャスター",
            Job.GrandPriest => "グランドプリースト",
            Job.GrandRanger => "グランドレンジャー",
            Job.Shogun => "大将軍",
            Job.Archmage => "大魔法使い",
            Job.GreatThief => "大盗賊",
            Job.GreatKnight => "大騎士長",
            Job.Bushin => "武神",
            Job.Seikaiou => "星界王",
            Job.Matouou => "魔盗王",
            Job.Shugoshin => "守護神",
            _ => job.ToString()
        };
}
