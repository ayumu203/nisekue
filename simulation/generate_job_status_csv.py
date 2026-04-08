#!/usr/bin/env python3
from __future__ import annotations

import csv
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
GROWTH_CSV = ROOT / "server" / "resources" / "player" / "job_levelup_growths.csv"
OUTPUT_DIR = ROOT / "simulation"


# Matches the player creation defaults in server/endpoints/PlayerEndpoints.cs.
INITIAL_STATUS = {
    "max_hp": 24,
    "max_mp": 8,
    "strength": 7,
    "defense": 5,
    "intelligence": 5,
    "luck": 3,
    "speed": 4,
}


BASE_JOBS = ["Warrior", "Guardian", "Mage", "Priest", "Ranger"]
ADVANCED_JOBS = {
    "OniWarrior": "Warrior",
    "Trickster": "Guardian",
    "FireMage": "Mage",
    "HighPriest": "Priest",
    "Sniper": "Ranger",
}
GRAND_JOBS = {
    "GrandWarrior": "OniWarrior",
    "GrandGuard": "Trickster",
    "GrandCaster": "FireMage",
    "GrandPriest": "HighPriest",
    "GrandRanger": "Sniper",
}
DAI_JOBS = {
    "Shogun": "GrandWarrior",
    "Archmage": "GrandCaster",
    "GreatThief": "GrandRanger",
}


@dataclass(frozen=True)
class JobGrowth:
    code: str
    name: str
    growth: dict[str, int]


def load_job_growths() -> dict[str, JobGrowth]:
    growths: dict[str, JobGrowth] = {}
    with GROWTH_CSV.open("r", encoding="utf-8", newline="") as fp:
        for row in csv.DictReader(fp):
            growths[row["job_code"]] = JobGrowth(
                code=row["job_code"],
                name=row["job_name"],
                growth={
                    "max_hp": int(row["max_hp"]),
                    "max_mp": int(row["max_mp"]),
                    "strength": int(row["strength"]),
                    "defense": int(row["defense"]),
                    "intelligence": int(row["intelligence"]),
                    "luck": int(row["luck"]),
                    "speed": int(row["speed"]),
                },
            )
    return growths


def clone_status(status: dict[str, int]) -> dict[str, int]:
    return {key: int(value) for key, value in status.items()}


def apply_growth(status: dict[str, int], growth: JobGrowth, times: int) -> dict[str, int]:
    next_status = clone_status(status)
    for _ in range(times):
        for key, value in growth.growth.items():
            next_status[key] += value
    return next_status


def build_rows_for_job(
    *,
    job: JobGrowth,
    start_status: dict[str, int],
    start_level: int,
    end_level: int,
    sample_levels: set[int],
    phase_start_level: int,
) -> list[dict[str, int | str]]:
    rows: list[dict[str, int | str]] = []
    current_status = clone_status(start_status)

    for level in range(start_level, end_level + 1):
        if level > start_level:
            current_status = apply_growth(current_status, job, 1)

        if level in sample_levels:
            rows.append(
                {
                    "job_code": job.code,
                    "job_name": job.name,
                    "player_level": level,
                    "job_level": (level - phase_start_level) + 1,
                    "max_hp": current_status["max_hp"],
                    "max_mp": current_status["max_mp"],
                    "strength": current_status["strength"],
                    "defense": current_status["defense"],
                    "intelligence": current_status["intelligence"],
                    "luck": current_status["luck"],
                    "speed": current_status["speed"],
                }
            )

    return rows


def write_csv(path: Path, rows: list[dict[str, int | str]]) -> None:
    fieldnames = [
        "job_code",
        "job_name",
        "player_level",
        "job_level",
        "max_hp",
        "max_mp",
        "strength",
        "defense",
        "intelligence",
        "luck",
        "speed",
    ]
    with path.open("w", encoding="utf-8", newline="") as fp:
        writer = csv.DictWriter(fp, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    growths = load_job_growths()

    base_rows: list[dict[str, int | str]] = []
    base_status_at_100: dict[str, dict[str, int]] = {}
    for job_code in BASE_JOBS:
        job = growths[job_code]
        rows = build_rows_for_job(
            job=job,
            start_status=INITIAL_STATUS,
            start_level=1,
            end_level=100,
            sample_levels=set(range(10, 101, 10)),
            phase_start_level=1,
        )
        base_rows.extend(rows)
        base_status_at_100[job_code] = {
            "max_hp": int(rows[-1]["max_hp"]),
            "max_mp": int(rows[-1]["max_mp"]),
            "strength": int(rows[-1]["strength"]),
            "defense": int(rows[-1]["defense"]),
            "intelligence": int(rows[-1]["intelligence"]),
            "luck": int(rows[-1]["luck"]),
            "speed": int(rows[-1]["speed"]),
        }

    advanced_rows: list[dict[str, int | str]] = []
    advanced_status_at_200: dict[str, dict[str, int]] = {}
    for advanced_code, base_code in ADVANCED_JOBS.items():
        job = growths[advanced_code]
        rows = build_rows_for_job(
            job=job,
            start_status=base_status_at_100[base_code],
            start_level=100,
            end_level=200,
            sample_levels=set(range(100, 201, 10)),
            phase_start_level=100,
        )
        advanced_rows.extend(rows)
        advanced_status_at_200[advanced_code] = {
            "max_hp": int(rows[-1]["max_hp"]),
            "max_mp": int(rows[-1]["max_mp"]),
            "strength": int(rows[-1]["strength"]),
            "defense": int(rows[-1]["defense"]),
            "intelligence": int(rows[-1]["intelligence"]),
            "luck": int(rows[-1]["luck"]),
            "speed": int(rows[-1]["speed"]),
        }

    grand_rows: list[dict[str, int | str]] = []
    grand_status_at_300: dict[str, dict[str, int]] = {}
    for grand_code, advanced_code in GRAND_JOBS.items():
        job = growths[grand_code]
        rows = build_rows_for_job(
            job=job,
            start_status=advanced_status_at_200[advanced_code],
            start_level=200,
            end_level=300,
            sample_levels=set(range(200, 301, 10)),
            phase_start_level=200,
        )
        grand_rows.extend(rows)
        grand_status_at_300[grand_code] = {
            "max_hp": int(rows[-1]["max_hp"]),
            "max_mp": int(rows[-1]["max_mp"]),
            "strength": int(rows[-1]["strength"]),
            "defense": int(rows[-1]["defense"]),
            "intelligence": int(rows[-1]["intelligence"]),
            "luck": int(rows[-1]["luck"]),
            "speed": int(rows[-1]["speed"]),
        }

    dai_rows: list[dict[str, int | str]] = []
    for dai_code, grand_code in DAI_JOBS.items():
        job = growths[dai_code]
        rows = build_rows_for_job(
            job=job,
            start_status=grand_status_at_300[grand_code],
            start_level=300,
            end_level=10000,
            sample_levels=set(range(1000, 10001, 1000)),
            phase_start_level=300,
        )
        dai_rows.extend(rows)

    write_csv(OUTPUT_DIR / "base_jobs_lv10_to_100.csv", base_rows)
    write_csv(OUTPUT_DIR / "advanced_jobs_lv100_to_200.csv", advanced_rows)
    write_csv(OUTPUT_DIR / "grand_jobs_lv200_to_300.csv", grand_rows)
    write_csv(OUTPUT_DIR / "dai_jobs_lv1000_to_10000.csv", dai_rows)


if __name__ == "__main__":
    main()
