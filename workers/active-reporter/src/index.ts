const GAME_KEY = 'nisekue'

export interface Env {
  BACKEND_URL: string
  MAINTENANCE_TOKEN: string
  REPORT_ENDPOINT_URL: string
  PORTAL_API_KEY: string
}

export default {
  async scheduled(_event: ScheduledEvent, env: Env, _ctx: ExecutionContext): Promise<void> {
    const countUrl = `${env.BACKEND_URL}/internal/active-player/count`
    const countResponse = await fetch(countUrl, {
      headers: { 'X-Maintenance-Token': env.MAINTENANCE_TOKEN },
    })
    if (!countResponse.ok) {
      throw new Error(`count request failed: ${countResponse.status} ${countResponse.statusText}`)
    }
    const { count } = await countResponse.json<{ count: number }>()

    const reportUrl = new URL(env.REPORT_ENDPOINT_URL)
    reportUrl.searchParams.set('game_key', GAME_KEY)
    reportUrl.searchParams.set('api_key', env.PORTAL_API_KEY)
    reportUrl.searchParams.set('online_count', String(count))

    const reportResponse = await fetch(reportUrl.toString())
    if (!reportResponse.ok) {
      throw new Error(`report request failed: ${reportResponse.status} ${reportResponse.statusText}`)
    }
  },
}
