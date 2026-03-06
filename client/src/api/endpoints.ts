import {
  getPlayerResponseSchema,
  createPlayerRequestSchema,
  createPlayerResponseSchema,
  getChatRoomRequestSchema,
  getChatRoomResponseSchema,
  postChatMessageRequestSchema,
  postChatMessageResponseSchema,
} from '../schema/player'
export type {
  GetPlayerResponse,
  CreatePlayerRequest,
  CreatePlayerResponse,
  GetChatRoomRequest,
  GetChatRoomResponse,
  PostChatMessageRequest,
  PostChatMessageResponse,
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
} as const
