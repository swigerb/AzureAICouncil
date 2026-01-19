import type { CouncilorStatement } from '../types'

function snippet(s: string, max = 180) {
  const trimmed = s.trim().replace(/\s+/g, ' ')
  return trimmed.length > max ? `${trimmed.slice(0, max)}…` : trimmed
}

export function DeliberationTimeline(props: { statements: CouncilorStatement[] }) {
  const byRound = new Map<number, CouncilorStatement[]>()
  for (const st of props.statements) {
    const arr = byRound.get(st.round) ?? []
    arr.push(st)
    byRound.set(st.round, arr)
  }

  const rounds = [...byRound.entries()].sort((a, b) => a[0] - b[0])

  return (
    <div className="card">
      <h2>Deliberation Timeline</h2>
      {rounds.length === 0 ? (
        <div className="muted">No statements yet.</div>
      ) : (
        rounds.map(([round, statements]) => (
          <div key={round} className="roundBlock">
            <h3>Round {round}</h3>
            <div className="timelineGrid">
              {statements.map((s) => (
                <div key={`${s.round}-${s.councilor}`} className="timelineItem">
                  <div className="timelineHeader">
                    <div className="councilorName">{s.councilor}</div>
                    <div className="muted">{s.stanceTitle}</div>
                  </div>
                  <div className="timelineBody">{snippet(s.answer)}</div>
                </div>
              ))}
            </div>
          </div>
        ))
      )}
    </div>
  )
}
