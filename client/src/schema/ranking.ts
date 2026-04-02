import { z } from 'zod'

export const rankingJobSchema = z.object({
  code: z.string().min(1),
  displayName: z.string().min(1),
})

export const rankingPlayerSchema = z.object({
  userId: z.string().uuid('ユーザーIDの形式が不正です'),
  userName: z.string().min(1).nullable().optional(),
  imagePath: z.string().min(1).nullable().optional(),
  level: z.number().int().nonnegative(),
  job: rankingJobSchema,
  rebirthCount: z.number().int().nonnegative(),
})

export const rankingRowSchema = z.object({
  rankingType: z.string().min(1),
  periodKind: z.enum(['Total', 'Weekly', 'Daily']),
  combatIndexRank: z.string().min(1).nullable(),
  rankPosition: z.number().int().min(1),
  score: z.number().int().nonnegative(),
  player: rankingPlayerSchema,
})

export const getRankingsResponseSchema = z.object({
  snapshotAt: z.string().datetime({ offset: true }).nullable(),
  rows: z.array(rankingRowSchema),
})

export type RankingRow = z.infer<typeof rankingRowSchema>
export type GetRankingsResponse = z.infer<typeof getRankingsResponseSchema>
