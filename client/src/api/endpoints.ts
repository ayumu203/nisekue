import { getPlayerResponseSchema, createPlayerRequestSchema, createPlayerResponseSchema } from '../schema/player'
export type {
  GetPlayerResponse,
  CreatePlayerRequest,
  CreatePlayerResponse,
} from '../schema/player'

export const endpoints = {
  player: {
    get: {
      path: '/player',
      method: 'GET',
      responseSchema: getPlayerResponseSchema,
    },
    create: {
      path: '/player',
      method: 'POST',
      requestSchema: createPlayerRequestSchema,
      responseSchema: createPlayerResponseSchema,
    },
  },
} as const
