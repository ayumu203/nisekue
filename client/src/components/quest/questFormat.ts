import type { GetQuestStagesResponse } from '@/schema/quest'

export function formatStagePartyRange(stage: GetQuestStagesResponse[number]): string {
  return `${stage.minPartyMemberCount} - ${stage.maxPartyMemberCount}`
}

export function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat('ja-JP', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}
