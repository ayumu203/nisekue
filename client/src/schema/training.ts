import { z } from 'zod'

export const trainingEnemySchema = z.object({
  id: z.number().int().min(1, 'enemyIdは1以上である必要があります'),
  name: z.string().trim().min(1, '敵名が不正です'),
  imagePath: z.string().trim().min(1, '画像パスが不正です'),
  level: z.number().int().min(1, '敵レベルは1以上である必要があります'),
})

export const getTrainingEnemiesResponseSchema = z.array(trainingEnemySchema)

export const executeTrainingRequestSchema = z.object({
  enemyId: z.number().int().min(1, 'enemyIdは1以上である必要があります'),
})

export const executeTrainingResponseSchema = z.object({
  trainingResult: z.enum(['Win', 'Lose', 'Draw']),
  turn: z.number().int().min(0, 'ターン数は0以上である必要があります'),
  currentPlayerHp: z.number().int().min(0, '現在プレイヤーHPは0以上である必要があります'),
  maxPlayerHp: z.number().int().min(1, 'プレイヤー最大HPは1以上である必要があります'),
  currentEnemyHp: z.number().int().min(0, '現在敵HPは0以上である必要があります'),
  maxEnemyHp: z.number().int().min(1, '敵最大HPは1以上である必要があります'),
  exp: z.number().int().min(0, '獲得経験値は0以上である必要があります'),
  isLevelUp: z.boolean(),
})

export const trainingCooldownErrorSchema = z.object({
  message: z.string().trim().min(1, 'エラーメッセージが不正です'),
  retryAfterSeconds: z.number().int().min(1, '再試行秒数は1以上である必要があります'),
  cooldownUntil: z.string().datetime({ offset: true, message: '日時の形式が不正です' }),
})

export type TrainingEnemy = z.infer<typeof trainingEnemySchema>
export type GetTrainingEnemiesResponse = z.infer<typeof getTrainingEnemiesResponseSchema>
export type ExecuteTrainingRequest = z.infer<typeof executeTrainingRequestSchema>
export type ExecuteTrainingResponse = z.infer<typeof executeTrainingResponseSchema>
export type TrainingCooldownError = z.infer<typeof trainingCooldownErrorSchema>
