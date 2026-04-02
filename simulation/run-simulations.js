#!/usr/bin/env node
const fs = require("fs");
const path = require("path");

const JOB_GROWTHS_CSV = `id,job_code,job_name,description,master_level,max_hp,max_mp,strength,defense,intelligence,luck,speed,required_master_job_1,required_master_job_2,required_master_job_3
1,Apprentice,見習い,旅立ったばかりの冒険者の基本職。HPとMPをまんべんなく伸ばしながら、まずは一体ずつ確実に打ち倒す戦い方を覚えていく。,5,4,2,2,2,2,1,1,,,
2,Warrior,戦士,剣を掲げて先頭に立つ王道の前衛職。HPと筋力が大きく伸び、重い一撃とまとめて薙ぎ払う攻めで敵陣を押し込み、そのまま力でねじ伏せる。,20,6,1,3,2,0,1,1,,,
3,Guardian,盾使い,鉄壁の守りで仲間の前に立つ防衛の前衛職。HPと防御が大きく伸び、堅さを活かした反撃と敵の狙いを引き受ける立ち回りで味方を守り抜く。,20,7,1,2,4,0,1,1,,,
4,Mage,魔法使い,強力な魔力で戦場を支配する攻撃魔法職。MPと知力が大きく伸び、単体を焼き払う呪文と複数を巻き込む魔法で後列から一気に勝負を決める。,20,3,4,0,1,4,1,1,,,
5,Priest,僧侶,神々への祈りで仲間に加護を授ける支援の職。MPと知力がよく伸び、傷を癒やしながら守りを固め、ときには祈りで味方の攻めを後押しして戦いを支える。,20,4,4,0,1,3,2,1,,,
6,Ranger,レンジャー,素早く立ち回り獲物を追い詰めるスキル巧派の職。素早さと運が伸びやすく、敵の動きを封じたりじわじわ追い込んだりして戦況を有利に傾ける。,20,5,2,2,1,1,2,3,,,
7,OniWarrior,鬼武者,鬼神のごとき猛攻で敵陣を断ち割る豪剣職。HPと筋力が大きく伸び、身を削るほど凶悪な一撃で前線を突破する。,30,8,1,6,1,0,1,3,Warrior,,
8,SwordMaster,ソードマスター,研ぎ澄まされた剣閃で敵を断つ達人の剣士。筋力と素早さがよく伸び、鋭い列攻撃と会心の一太刀で勝機を掴む。,30,7,1,5,2,0,1,4,Warrior,,
9,Trickster,トリックスター,幻惑と奇策で戦場をかき乱す撹乱盾役。HPと素早さが伸び、狙いを集めつつ身かわしと軽減で粘り強く立つ。,30,7,2,2,3,0,2,5,Guardian,,
10,Crusader,クルセイダー,聖なる盾で仲間を守る守護騎士。HPと防御が大きく伸び、堅牢な挑発と加護で味方を護り抜く。,30,8,2,2,6,1,1,1,Guardian,,
11,FireMage,火魔法使い,灼熱の術を極めた炎の魔導士。MPと知力が大きく伸び、列を焼く火炎呪文で敵陣を崩す。,30,4,6,0,1,6,1,2,Mage,,
12,WaterMage,水魔法使い,奔流と雨を操る水の魔導士。MPと知力がよく伸び、広い範囲へ安定した水魔法を叩き込む。,30,5,6,0,2,5,1,2,Mage,,
13,WindMage,風魔法使い,暴風を呼ぶ風の魔導士。知力と素早さが伸び、全体を巻き込む風魔法で一気に制圧する。,30,4,6,0,1,5,2,4,Mage,,
14,HighPriest,神官,より高位の加護を司る聖職者。MPと知力が伸び、列回復と再生の祈りで味方の戦線を維持する。,30,5,6,0,1,5,2,1,Priest,,
15,Necromancer,死霊使い,死の気配と亡者の力を操る禁忌の術師。筋力と知力を併せ持ち、即死呪法と死霊召喚で敵を追い詰める。,30,5,5,3,1,5,2,1,Priest,,
16,Sniper,スナイパー,一撃必殺を狙う狙撃の名手。筋力と素早さと運が伸び、毒矢と急所撃ちで確実に獲物を仕留める。,30,5,2,4,1,1,4,5,Ranger,,
17,TrapMaster,罠師,罠と霧で戦場を支配する策士。MPと素早さが伸び、麻痺罠と黒霧で敵の行動精度を崩す。,30,5,4,2,2,3,2,4,Ranger,,
18,GrandWarrior,グランドウォリアー,覇道の剣気で敵陣を断ち砕く豪勇の剣王。筋力と素早さがさらに伸び、列を薙ぐ斬撃と血戦の覚悟で前線を押し潰す。,40,9,2,7,3,0,1,5,OniWarrior,SwordMaster,
19,GrandGuard,グランドガード,絶対の守護を掲げて仲間すべてを庇う守城騎士。HPと防御が大きく伸び、祈りと城壁の構えで盤石の戦線を築く。,40,9,4,1,7,3,1,2,SwordMaster,Trickster,
20,GrandCaster,グランドキャスター,三大精霊を束ねる大魔導の体現者。MPと知力が大きく伸び、広域殲滅と魔力転成で戦場を支配する。,40,6,7,0,1,7,1,5,FireMage,WaterMage,WindMage
21,GrandPriest,グランドプリースト,生死の境を越えて加護を与える大神官。MPと知力が伸び、全体回復と蘇生結界で味方を立て直す。,40,7,7,0,2,6,2,1,HighPriest,Necromancer,
22,GrandRanger,グランドレンジャー,無数の罠と死角の狙撃で獲物を逃がさぬ狩猟王。筋力と運と素早さが伸び、全体罠と即死狙撃で敵軍を崩す。,40,7,3,5,2,2,5,5,Sniper,TrapMaster,
23,Shogun,大将軍,万軍を率いる覇者の将。全能力が高く伸び、己を鼓舞してから放つ一閃で敵主力を討ち果たす。,50,9,4,6,6,3,3,3,GrandWarrior,GrandGuard,
24,Archmage,大魔法使い,星々の理を編み替える究極の魔導師。MPと知力が極めて伸び、終末級魔法と超回復で戦局を塗り替える。,50,6,9,0,2,8,2,4,GrandCaster,GrandPriest,
25,GreatThief,大盗賊,戦場と財宝の匂いを嗅ぎ分ける伝説の盗賊王。運と素早さが大きく伸び、首狩りと災厄の紫煙で獲物も報酬も逃がさない。,50,7,4,4,2,3,6,6,Warrior,WindMage,GrandRanger`;

const STAT_KEYS = ["maxHp", "maxMp", "strength", "defense", "intelligence", "luck", "speed"];
const COMBAT_WEIGHTS = {
  maxHp: 0.3,
  maxMp: 0.15,
  strength: 1.2,
  defense: 1.1,
  intelligence: 1.05,
  luck: 0.8,
  speed: 0.9
};

const PATTERNS = [
  { label: "レベル5: 旅人5", stages: [{ jobId: 1, level: 5 }] },
  { label: "レベル10: 旅人5→戦士5", stages: [{ jobId: 1, level: 5 }, { jobId: 2, level: 5 }] },
  { label: "レベル10: 旅人5→盾使い5", stages: [{ jobId: 1, level: 5 }, { jobId: 3, level: 5 }] },
  { label: "レベル10: 旅人5→魔法使い5", stages: [{ jobId: 1, level: 5 }, { jobId: 4, level: 5 }] },
  { label: "レベル10: 旅人5→僧侶5", stages: [{ jobId: 1, level: 5 }, { jobId: 5, level: 5 }] },
  { label: "レベル10: 旅人5→レンジャー5", stages: [{ jobId: 1, level: 5 }, { jobId: 6, level: 5 }] },
  { label: "レベル25: 旅人5→戦士20", stages: [{ jobId: 1, level: 5 }, { jobId: 2, level: 20 }] },
  { label: "レベル25: 旅人5→盾使い20", stages: [{ jobId: 1, level: 5 }, { jobId: 3, level: 20 }] },
  { label: "レベル25: 旅人5→魔法使い20", stages: [{ jobId: 1, level: 5 }, { jobId: 4, level: 20 }] },
  { label: "レベル25: 旅人5→僧侶20", stages: [{ jobId: 1, level: 5 }, { jobId: 5, level: 20 }] },
  { label: "レベル25: 旅人5→レンジャー20", stages: [{ jobId: 1, level: 5 }, { jobId: 6, level: 20 }] },
  { label: "レベル50: 旅人5→戦士45", stages: [{ jobId: 1, level: 5 }, { jobId: 2, level: 45 }] },
  { label: "レベル50: 旅人5→盾使い45", stages: [{ jobId: 1, level: 5 }, { jobId: 3, level: 45 }] },
  { label: "レベル50: 旅人5→魔法使い45", stages: [{ jobId: 1, level: 5 }, { jobId: 4, level: 45 }] },
  { label: "レベル50: 旅人5→僧侶45", stages: [{ jobId: 1, level: 5 }, { jobId: 5, level: 45 }] },
  { label: "レベル50: 旅人5→レンジャー45", stages: [{ jobId: 1, level: 5 }, { jobId: 6, level: 45 }] },
  { label: "レベル75: 旅人5→戦士65", stages: [{ jobId: 1, level: 5 }, { jobId: 2, level: 65 }] },
  { label: "レベル75: 旅人5→盾使い65", stages: [{ jobId: 1, level: 5 }, { jobId: 3, level: 65 }] },
  { label: "レベル75: 旅人5→魔法使い65", stages: [{ jobId: 1, level: 5 }, { jobId: 4, level: 65 }] },
  { label: "レベル75: 旅人5→僧侶65", stages: [{ jobId: 1, level: 5 }, { jobId: 5, level: 65 }] },
  { label: "レベル75: 旅人5→レンジャー65", stages: [{ jobId: 1, level: 5 }, { jobId: 6, level: 65 }] },
  { label: "レベル100: 旅人5→戦士65→鬼武者30", stages: [{ jobId: 1, level: 5 }, { jobId: 2, level: 65 }, { jobId: 7, level: 30 }] },
  { label: "レベル100: 旅人5→戦士65→ソードマスター30", stages: [{ jobId: 1, level: 5 }, { jobId: 2, level: 65 }, { jobId: 8, level: 30 }] },
  { label: "レベル100: 旅人5→盾使い65→トリックスター30", stages: [{ jobId: 1, level: 5 }, { jobId: 3, level: 65 }, { jobId: 9, level: 30 }] },
  { label: "レベル100: 旅人5→盾使い65→クルセイダー30", stages: [{ jobId: 1, level: 5 }, { jobId: 3, level: 65 }, { jobId: 10, level: 30 }] },
  { label: "レベル100: 旅人5→魔法使い65→火魔法使い30", stages: [{ jobId: 1, level: 5 }, { jobId: 4, level: 65 }, { jobId: 11, level: 30 }] },
  { label: "レベル100: 旅人5→魔法使い65→水魔法使い30", stages: [{ jobId: 1, level: 5 }, { jobId: 4, level: 65 }, { jobId: 12, level: 30 }] },
  { label: "レベル100: 旅人5→魔法使い65→風魔法使い30", stages: [{ jobId: 1, level: 5 }, { jobId: 4, level: 65 }, { jobId: 13, level: 30 }] },
  { label: "レベル100: 旅人5→僧侶65→神官30", stages: [{ jobId: 1, level: 5 }, { jobId: 5, level: 65 }, { jobId: 14, level: 30 }] },
  { label: "レベル100: 旅人5→僧侶65→死霊使い30", stages: [{ jobId: 1, level: 5 }, { jobId: 5, level: 65 }, { jobId: 15, level: 30 }] },
  { label: "レベル100: 旅人5→レンジャー65→スナイパー30", stages: [{ jobId: 1, level: 5 }, { jobId: 6, level: 65 }, { jobId: 16, level: 30 }] },
  { label: "レベル100: 旅人5→レンジャー65→罠師30", stages: [{ jobId: 1, level: 5 }, { jobId: 6, level: 65 }, { jobId: 17, level: 30 }] },
  { label: "レベル150: 旅人5→戦士65→鬼武者75", stages: [{ jobId: 1, level: 5 }, { jobId: 2, level: 65 }, { jobId: 7, level: 75 }] },
  { label: "レベル150: 旅人5→戦士65→ソードマスター75", stages: [{ jobId: 1, level: 5 }, { jobId: 2, level: 65 }, { jobId: 8, level: 75 }] },
  { label: "レベル150: 旅人5→盾使い65→トリックスター75", stages: [{ jobId: 1, level: 5 }, { jobId: 3, level: 65 }, { jobId: 9, level: 75 }] },
  { label: "レベル150: 旅人5→盾使い65→クルセイダー75", stages: [{ jobId: 1, level: 5 }, { jobId: 3, level: 65 }, { jobId: 10, level: 75 }] },
  { label: "レベル150: 旅人5→魔法使い65→火魔法使い75", stages: [{ jobId: 1, level: 5 }, { jobId: 4, level: 65 }, { jobId: 11, level: 75 }] },
  { label: "レベル150: 旅人5→魔法使い65→水魔法使い75", stages: [{ jobId: 1, level: 5 }, { jobId: 4, level: 65 }, { jobId: 12, level: 75 }] },
  { label: "レベル150: 旅人5→魔法使い65→風魔法使い75", stages: [{ jobId: 1, level: 5 }, { jobId: 4, level: 65 }, { jobId: 13, level: 75 }] },
  { label: "レベル150: 旅人5→僧侶65→神官75", stages: [{ jobId: 1, level: 5 }, { jobId: 5, level: 65 }, { jobId: 14, level: 75 }] },
  { label: "レベル150: 旅人5→僧侶65→死霊使い75", stages: [{ jobId: 1, level: 5 }, { jobId: 5, level: 65 }, { jobId: 15, level: 75 }] },
  { label: "レベル150: 旅人5→レンジャー65→スナイパー75", stages: [{ jobId: 1, level: 5 }, { jobId: 6, level: 65 }, { jobId: 16, level: 75 }] },
  { label: "レベル150: 旅人5→レンジャー65→罠師75", stages: [{ jobId: 1, level: 5 }, { jobId: 6, level: 65 }, { jobId: 17, level: 75 }] },
  { label: "レベル200: 旅人5→戦士65→鬼武者75→ソードマスター50", stages: [{ jobId: 1, level: 5 }, { jobId: 2, level: 65 }, { jobId: 7, level: 75 }, { jobId: 8, level: 50 }] },
  { label: "レベル200: 旅人5→盾使い65→トリックスター75→クルセイダー50", stages: [{ jobId: 1, level: 5 }, { jobId: 3, level: 65 }, { jobId: 9, level: 75 }, { jobId: 10, level: 50 }] },
  { label: "レベル200: 旅人5→魔法使い65→火魔法使い75→水魔法使い50", stages: [{ jobId: 1, level: 5 }, { jobId: 4, level: 65 }, { jobId: 11, level: 75 }, { jobId: 12, level: 50 }] },
  { label: "レベル200: 旅人5→僧侶65→神官75→死霊使い50", stages: [{ jobId: 1, level: 5 }, { jobId: 5, level: 65 }, { jobId: 14, level: 75 }, { jobId: 15, level: 50 }] },
  { label: "レベル200: 旅人5→レンジャー65→スナイパー75→罠師50", stages: [{ jobId: 1, level: 5 }, { jobId: 6, level: 65 }, { jobId: 16, level: 75 }, { jobId: 17, level: 50 }] },
  { label: "レベル300: 旅人5→戦士65→鬼武者75→ソードマスター30→グランドウォーリアー170", stages: [{ jobId: 1, level: 5 }, { jobId: 2, level: 65 }, { jobId: 7, level: 75 }, { jobId: 8, level: 30 }, { jobId: 18, level: 170 }] },
  { label: "レベル300: 旅人5→盾使い65→トリックスター75→クルセイダー30→グランドガード170", stages: [{ jobId: 1, level: 5 }, { jobId: 3, level: 65 }, { jobId: 9, level: 75 }, { jobId: 10, level: 30 }, { jobId: 19, level: 170 }] },
  { label: "レベル300: 旅人5→魔法使い65→火魔法使い75→水魔法使い30→風魔法使い30→グランドキャスター40", stages: [{ jobId: 1, level: 5 }, { jobId: 4, level: 65 }, { jobId: 11, level: 75 }, { jobId: 12, level: 30 }, { jobId: 13, level: 30 }, { jobId: 20, level: 40 }] },
  { label: "レベル300: 旅人5→僧侶65→神官75→死霊使い30→グランドプリースト170", stages: [{ jobId: 1, level: 5 }, { jobId: 5, level: 65 }, { jobId: 14, level: 75 }, { jobId: 15, level: 30 }, { jobId: 21, level: 170 }] },
  { label: "レベル300: 旅人5→レンジャー65→スナイパー75→罠師30→グランドレンジャー170", stages: [{ jobId: 1, level: 5 }, { jobId: 6, level: 65 }, { jobId: 16, level: 75 }, { jobId: 17, level: 30 }, { jobId: 22, level: 170 }] }
];

function main() {
  const jobs = parseJobCsv(JOB_GROWTHS_CSV);
  const jobMap = Object.fromEntries(jobs.map((job) => [job.id, job]));

  const results = PATTERNS.map((pattern) => {
    const growth = zeroStats();
    for (const stage of pattern.stages) {
      const job = jobMap[stage.jobId];
      if (!job) {
        throw new Error(`unknown job id ${stage.jobId} used in pattern ${pattern.label}`);
      }
      for (const key of STAT_KEYS) {
        growth[key] += job.growth[key] * stage.level;
      }
    }

    const finalStats = { ...growth };
    const combatIndex = calculateCombatIndex(finalStats);
    const totalLevel = pattern.stages.reduce((sum, stage) => sum + stage.level, 0);

    return {
      label: pattern.label,
      totalLevel,
      stats: finalStats,
      combatIndex
    };
  });

  const csvLines = [
    [
      "label",
      "total_level",
      "max_hp",
      "max_mp",
      "strength",
      "defense",
      "intelligence",
      "luck",
      "speed",
      "combat_index"
    ].join(",")
  ];

  for (const row of results) {
    csvLines.push([
      csvEscape(row.label),
      row.totalLevel,
      row.stats.maxHp,
      row.stats.maxMp,
      row.stats.strength,
      row.stats.defense,
      row.stats.intelligence,
      row.stats.luck,
      row.stats.speed,
      row.combatIndex
    ].join(","));
  }

  const outputPath = path.join(__dirname, "status_simulation_result.csv");
  fs.writeFileSync(outputPath, csvLines.join("\n"));
  console.log(`書き出した結果: ${outputPath}`);
}

function parseJobCsv(text) {
  const lines = text.trim().split(/\r?\n/);
  const dataLines = lines.slice(1);
  return dataLines.map((line) => {
    const cols = line.split(",");
    return {
      id: number(cols[0], 0),
      growth: {
        maxHp: number(cols[5], 0),
        maxMp: number(cols[6], 0),
        strength: number(cols[7], 0),
        defense: number(cols[8], 0),
        intelligence: number(cols[9], 0),
        luck: number(cols[10], 0),
        speed: number(cols[11], 0)
      }
    };
  });
}

function calculateCombatIndex(stats) {
  return Math.floor(
    stats.maxHp * COMBAT_WEIGHTS.maxHp +
      stats.maxMp * COMBAT_WEIGHTS.maxMp +
      stats.strength * COMBAT_WEIGHTS.strength +
      stats.defense * COMBAT_WEIGHTS.defense +
      stats.intelligence * COMBAT_WEIGHTS.intelligence +
      stats.luck * COMBAT_WEIGHTS.luck +
      stats.speed * COMBAT_WEIGHTS.speed
  );
}

function zeroStats() {
  return {
    maxHp: 0,
    maxMp: 0,
    strength: 0,
    defense: 0,
    intelligence: 0,
    luck: 0,
    speed: 0
  };
}

function number(value, fallback) {
  const n = Number(value);
  return Number.isFinite(n) ? n : fallback;
}

function csvEscape(value) {
  const text = String(value ?? "");
  return `"${text.replace(/"/g, '""')}"`;
}

main();
