import type {
  CouncilorStatement,
  CouncilResolution,
  CouncilorStatusUpdate,
  ErrorUpdate,
  RoundStartedUpdate,
  StreamEventName,
} from '../types'
import { apiBaseUrl } from './apiClient'

export type StreamHandlers = {
  onConnected?: () => void
  onRoundStarted?: (payload: RoundStartedUpdate) => void
  onCouncilorStatus?: (payload: CouncilorStatusUpdate) => void
  onStatement?: (payload: CouncilorStatement) => void
  onResolution?: (payload: CouncilResolution) => void
  onError?: (payload: ErrorUpdate) => void
}

export function connectDeliberationStream(deliberationId: string, handlers: StreamHandlers) {
  const url = `${apiBaseUrl()}/api/deliberations/${encodeURIComponent(deliberationId)}/stream`
  const es = new EventSource(url)

  let isConnected = false
  const markConnected = () => {
    if (isConnected) return
    isConnected = true
    handlers.onConnected?.()
  }

  const listen = <T>(event: StreamEventName, cb?: (payload: T) => void) => {
    if (!cb) return
    es.addEventListener(event, (e) => {
      markConnected()
      const msg = e as MessageEvent<string>
      cb(JSON.parse(msg.data) as T)
    })
  }

  listen<RoundStartedUpdate>('round.started', handlers.onRoundStarted)
  listen<CouncilorStatusUpdate>('councilor.status', handlers.onCouncilorStatus)
  listen<CouncilorStatement>('statement', handlers.onStatement)
  listen<CouncilResolution>('resolution', handlers.onResolution)
  listen<ErrorUpdate>('error', handlers.onError)

  // Basic connectivity checks
  es.addEventListener('stream.ready', () => {
    markConnected()
  })

  es.onerror = () => {
    // Browser will retry automatically; surface a lightweight error.
    handlers.onError?.({ where: 'sse', message: 'SSE connection error (browser will retry).', retryable: true })
  }

  return {
    close: () => es.close(),
  }
}
