import type { CouncilorStatusUpdate } from '../types'

export function StreamingStatus(props: {
  deliberationId?: string
  status: string
  councilorStatuses: Record<string, CouncilorStatusUpdate | undefined>
}) {
  return (
    <div className="card">
      <h2>Status</h2>
      <div className="muted">DeliberationId: {props.deliberationId ?? '—'}</div>
      <div className="statusPill">{props.status}</div>

      <div className="statusGrid">
        {Object.entries(props.councilorStatuses).map(([k, v]) => (
          <div key={k} className="statusRow">
            <div className="statusKey">{k}</div>
            <div className="muted">{v ? `round ${v.round}: ${v.state}` : '—'}</div>
          </div>
        ))}
      </div>
    </div>
  )
}
