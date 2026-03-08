import {
  getPlayerResponseSchema,
  createPlayerRequestSchema,
  createPlayerResponseSchema,
  updatePlayerNameRequestSchema,
  updatePlayerNameResponseSchema,
  updatePlayerJobRequestSchema,
  updatePlayerJobResponseSchema,
} from '@/schema/player'
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

export type {
  GetPlayerResponse,
  PlayerMoveSlot,
  CreatePlayerRequest,
  CreatePlayerResponse,
  UpdatePlayerNameRequest,
  UpdatePlayerNameResponse,
  UpdatePlayerJobRequest,
  UpdatePlayerJobResponse,
} from '@/schema/player'
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
    updateName: {
      path: '/player/name',
      method: 'PUT',
      requestSchema: updatePlayerNameRequestSchema,
      responseSchema: updatePlayerNameResponseSchema,
    },
    updateJob: {
      path: '/player/job',
      method: 'PUT',
      requestSchema: updatePlayerJobRequestSchema,
      responseSchema: updatePlayerJobResponseSchema,
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
