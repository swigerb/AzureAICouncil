import type { CouncilResolution } from '../types'

export function CouncilResolutionView(props: { resolution?: CouncilResolution | null }) {
  const r = props.resolution
  if (!r) {
    return (
      <div className="card">
        <h2>Council Resolution</h2>
        <div className="muted">Not issued yet.</div>
      </div>
    )
  }

  return (
    <div className="card">
      <h2>Council Resolution</h2>

      <div className="section">
        <h3>Summary</h3>
        <p>{r.summary}</p>
      </div>

      <div className="section">
        <h3>Recommendation</h3>
        <p>{r.recommendation}</p>
      </div>

      <div className="grid2">
        <div>
          <h3>Pros</h3>
          <ul>
            {r.pros.map((p, idx) => (
              <li key={idx}>{p}</li>
            ))}
          </ul>
        </div>
        <div>
          <h3>Cons</h3>
          <ul>
            {r.cons.map((c, idx) => (
              <li key={idx}>{c}</li>
            ))}
          </ul>
        </div>
      </div>

      <div className="section">
        <h3>Scores</h3>
        <div className="scoreRow">
          <span>risk:</span> <span className="mono">{r.scores.risk}</span>
          <span>cost:</span> <span className="mono">{r.scores.cost}</span>
          <span>complexity:</span> <span className="mono">{r.scores.complexity}</span>
          <span>confidence:</span> <span className="mono">{r.scores.confidence.toFixed(2)}</span>
        </div>
      </div>

      <div className="grid2">
        <div>
          <h3>When this is a good idea</h3>
          <ul>
            {r.whenThisIsAGoodIdea.map((x, idx) => (
              <li key={idx}>{x}</li>
            ))}
          </ul>
        </div>
        <div>
          <h3>When to avoid</h3>
          <ul>
            {r.whenToAvoid.map((x, idx) => (
              <li key={idx}>{x}</li>
            ))}
          </ul>
        </div>
      </div>

      <div className="section">
        <h3>Suggested next steps</h3>
        <ul>
          {r.suggestedNextSteps.map((x, idx) => (
            <li key={idx}>{x}</li>
          ))}
        </ul>
      </div>

      <div className="section">
        <h3>Mermaid</h3>
        <pre className="codeBlock">{r.mermaidArchitectureSnippet}</pre>
      </div>
    </div>
  )
}
