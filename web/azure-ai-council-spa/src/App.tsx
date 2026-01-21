import { useEffect, useMemo, useRef, useState } from 'react'
import './App.css'
import { MotionForm } from './components/MotionForm'
import { StreamingStatus } from './components/StreamingStatus'
import { DeliberationTimeline } from './components/DeliberationTimeline'
import { CouncilResolutionView } from './components/CouncilResolutionView'
import { createMotion, getDeliberation } from './services/apiClient'
import { connectDeliberationStream } from './services/streamClient'
import type { CouncilorStatusUpdate, CouncilorStatement, CreateMotionRequest, CouncilResolution } from './types'

function App() {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [deliberationId, setDeliberationId] = useState<string | undefined>()
  const [status, setStatus] = useState('Awaiting Council')
  const [statements, setStatements] = useState<CouncilorStatement[]>([])
  const [resolution, setResolution] = useState<CouncilResolution | null>(null)
  const [error, setError] = useState<string | undefined>()

  const [councilorStatuses, setCouncilorStatuses] = useState<Record<string, CouncilorStatusUpdate | undefined>>({
    Vision: undefined,
    Risk: undefined,
    Architecture: undefined,
    Security: undefined,
    Finance: undefined,
  })

  const streamRef = useRef<{ close: () => void } | null>(null)

  const sortedStatements = useMemo(() => {
    return [...statements].sort((a, b) => (a.round - b.round) || a.councilor.localeCompare(b.councilor))
  }, [statements])

  useEffect(() => {
    return () => {
      streamRef.current?.close()
      streamRef.current = null
    }
  }, [])

  return (
    <div className="page">
      <header className="header">
        <h1>Azure AI Council</h1>
        <div className="muted">Deliberative multi-agent decision system (v1)</div>
      </header>

      <div className="layout">
        <div className="col">
          <MotionForm
            disabled={isSubmitting}
            onSubmit={async (req: CreateMotionRequest) => {
              setError(undefined)
              setIsSubmitting(true)
              setStatus('Awaiting Council')
              setStatements([])
              setResolution(null)
              setCouncilorStatuses({ Vision: undefined, Risk: undefined, Architecture: undefined, Security: undefined, Finance: undefined })

              streamRef.current?.close()
              streamRef.current = null

              try {
                const resp = await createMotion(req)
                setDeliberationId(resp.deliberationId)

                // Load any already-produced state.
                const snapshot = await getDeliberation(resp.deliberationId)
                setStatus(snapshot.status)
                setStatements(snapshot.rounds.flatMap((r) => r.statements))
                setResolution(snapshot.resolution ?? null)

                streamRef.current = connectDeliberationStream(resp.deliberationId, {
                  onConnected: () => {
                    setError(undefined)
                  },
                  onRoundStarted: (p) => {
                    setStatus(`Deliberation Round ${p.round}`)
                  },
                  onCouncilorStatus: (p) => {
                    setCouncilorStatuses((prev) => ({ ...prev, [p.councilor]: p }))
                  },
                  onStatement: (p) => {
                    setStatements((prev) => {
                      const without = prev.filter((x) => !(x.round === p.round && x.councilor === p.councilor))
                      return [...without, p]
                    })
                  },
                  onResolution: (p) => {
                    setResolution(p)
                    setStatus('Resolution Issued')
                  },
                  onError: (p) => {
                    setError(`${p.where}: ${p.message}`)
                  },
                })
              } finally {
                setIsSubmitting(false)
              }
            }}
          />

          {error ? <div className="error">{error}</div> : null}
        </div>

        <div className="col">
          <StreamingStatus deliberationId={deliberationId} status={status} councilorStatuses={councilorStatuses} />
          <DeliberationTimeline statements={sortedStatements} />
          <CouncilResolutionView resolution={resolution} />
        </div>
      </div>
    </div>
  )
}

export default App
