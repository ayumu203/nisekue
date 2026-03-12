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
import {
  getQuestStagesResponseSchema,
  createQuestRoomRequestSchema,
  questRoomDetailResponseSchema,
  listQuestRoomsRequestSchema,
  listQuestRoomsResponseSchema,
  updateQuestRoomPositionRequestSchema,
  questRunDetailResponseSchema,
  submitQuestCommandRequestSchema,
  submitQuestCommandResponseSchema,
  manualControlRequestSchema,
  manualControlResponseSchema,
  postQuestChatMessageRequestSchema,
  postQuestChatMessageResponseSchema,
  questRunHubSnapshotEventSchema,
  questRunHubUpdatedEventSchema,
  questRunHubErrorEventSchema,
} from '@/schema/quest'

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
export type {
  GetQuestStagesResponse,
  CreateQuestRoomRequest,
  QuestRoomDetailResponse,
  ListQuestRoomsRequest,
  QuestRoomSummaryResponse,
  ListQuestRoomsResponse,
  QuestRunDetailResponse,
  SubmitQuestCommandRequest,
  SubmitQuestCommandResponse,
  ManualControlRequest,
  ManualControlResponse,
  PostQuestChatMessageRequest,
  PostQuestChatMessageResponse,
  QuestRunHubSnapshotEvent,
  QuestRunHubUpdatedEvent,
  QuestRunHubErrorEvent,
} from '@/schema/quest'

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
  quest: {
    getStages: {
      path: '/quest/stages',
      method: 'GET',
      responseSchema: getQuestStagesResponseSchema,
    },
    createRoom: {
      path: '/quest/rooms',
      method: 'POST',
      requestSchema: createQuestRoomRequestSchema,
      responseSchema: questRoomDetailResponseSchema,
    },
    listRooms: {
      path: '/quest/rooms',
      method: 'GET',
      requestSchema: listQuestRoomsRequestSchema,
      responseSchema: listQuestRoomsResponseSchema,
    },
    getRoom: {
      path: (roomId: string) => `/quest/rooms/${roomId}`,
      method: 'GET',
      responseSchema: questRoomDetailResponseSchema,
    },
    joinRoom: {
      path: (roomId: string) => `/quest/rooms/${roomId}/join`,
      method: 'POST',
      responseSchema: questRoomDetailResponseSchema,
    },
    updateRoomPosition: {
      path: (roomId: string) => `/quest/rooms/${roomId}/positions`,
      method: 'PUT',
      requestSchema: updateQuestRoomPositionRequestSchema,
      responseSchema: questRoomDetailResponseSchema,
    },
    startRoom: {
      path: (roomId: string) => `/quest/rooms/${roomId}/start`,
      method: 'POST',
      responseSchema: questRunDetailResponseSchema,
    },
    getRun: {
      path: (runId: string) => `/quest/runs/${runId}`,
      method: 'GET',
      responseSchema: questRunDetailResponseSchema,
    },
    submitCommand: {
      path: (runId: string) => `/quest/runs/${runId}/commands`,
      method: 'POST',
      requestSchema: submitQuestCommandRequestSchema,
      responseSchema: submitQuestCommandResponseSchema,
    },
    requestManualControl: {
      path: (runId: string) => `/quest/runs/${runId}/manual-control/request`,
      method: 'POST',
      requestSchema: manualControlRequestSchema,
      responseSchema: manualControlResponseSchema,
    },
    approveManualControl: {
      path: (runId: string) => `/quest/runs/${runId}/manual-control/approve`,
      method: 'POST',
      requestSchema: manualControlRequestSchema,
      responseSchema: manualControlResponseSchema,
    },
    postChatMessage: {
      path: (runId: string) => `/quest/runs/${runId}/chat`,
      method: 'POST',
      requestSchema: postQuestChatMessageRequestSchema,
      responseSchema: postQuestChatMessageResponseSchema,
    },
    hub: {
      path: '/quest-hubs/runs',
      snapshotEvent: 'QuestRunSnapshot',
      snapshotEventSchema: questRunHubSnapshotEventSchema,
      updatedEvent: 'QuestRunUpdated',
      updatedEventSchema: questRunHubUpdatedEventSchema,
      errorEvent: 'QuestRunError',
      errorEventSchema: questRunHubErrorEventSchema,
      subscribeMethod: 'SubscribeRun',
      unsubscribeMethod: 'UnsubscribeRun',
    },
  },
} as const
