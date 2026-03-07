import { getPlayerResponseSchema, createPlayerRequestSchema, createPlayerResponseSchema } from '@/schema/player'
import {
  getChatRoomRequestSchema,
  getChatRoomResponseSchema,
  postChatMessageRequestSchema,
  postChatMessageResponseSchema,
} from '@/schema/chat'
import {
  getTrainingEnemiesResponseSchema,
  executeTrainingRequestSchema,
  executeTrainingResponseSchema,
} from '@/schema/training'

export type { GetPlayerResponse, CreatePlayerRequest, CreatePlayerResponse } from '@/schema/player'
export type {
  GetChatRoomRequest,
  GetChatRoomResponse,
  PostChatMessageRequest,
  PostChatMessageResponse,
} from '@/schema/chat'
export type {
  TrainingEnemy,
  GetTrainingEnemiesResponse,
  ExecuteTrainingRequest,
  ExecuteTrainingResponse,
  TrainingCooldownError,
} from '@/schema/training'

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
  chatRoom: {
    get: {
      path: '/chat/room',
      method: 'GET',
      requestSchema: getChatRoomRequestSchema,
      responseSchema: getChatRoomResponseSchema,
    },
    postMessage: {
      path: '/chat/room/messages',
      method: 'POST',
      requestSchema: postChatMessageRequestSchema,
      responseSchema: postChatMessageResponseSchema,
    },
  },
  training: {
    getEnemies: {
      path: '/training/enemies',
      method: 'GET',
      responseSchema: getTrainingEnemiesResponseSchema,
    },
    execute: {
      path: '/training/execute',
      method: 'POST',
      requestSchema: executeTrainingRequestSchema,
      responseSchema: executeTrainingResponseSchema,
    },
  },
} as const
