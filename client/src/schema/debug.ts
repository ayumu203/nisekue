import { z } from 'zod'

export const getDebugStatusResponseSchema = z.object({
  canUseDebug: z.boolean(),
})

export type GetDebugStatusResponse = z.infer<typeof getDebugStatusResponseSchema>
