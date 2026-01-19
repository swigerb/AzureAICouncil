import { useMemo, useState } from 'react'
import type { CreateMotionRequest, RiskTolerance } from '../types'

export function MotionForm(props: { onSubmit: (req: CreateMotionRequest) => Promise<void>; disabled?: boolean }) {
  const [question, setQuestion] = useState('')
  const [budget, setBudget] = useState('')
  const [timeline, setTimeline] = useState('')
  const [riskTolerance, setRiskTolerance] = useState<RiskTolerance>('unspecified')
  const [techPrefs, setTechPrefs] = useState('')

  const parsedTechPrefs = useMemo(
    () =>
      techPrefs
        .split(',')
        .map((s) => s.trim())
        .filter(Boolean),
    [techPrefs],
  )

  return (
    <form
      onSubmit={async (e) => {
        e.preventDefault()
        await props.onSubmit({
          question,
          constraints: {
            budget: budget || undefined,
            timeline: timeline || undefined,
            riskTolerance,
            technologyPreferences: parsedTechPrefs.length ? parsedTechPrefs : undefined,
          },
        })
      }}
      className="card"
    >
      <h2>Submit a Motion</h2>

      <label>
        Question
        <textarea value={question} onChange={(e) => setQuestion(e.target.value)} rows={5} required />
      </label>

      <div className="grid2">
        <label>
          Budget (optional)
          <input value={budget} onChange={(e) => setBudget(e.target.value)} placeholder="e.g., under $200/mo" />
        </label>
        <label>
          Timeline (optional)
          <input value={timeline} onChange={(e) => setTimeline(e.target.value)} placeholder="e.g., 2 weeks" />
        </label>
      </div>

      <div className="grid2">
        <label>
          Risk tolerance
          <select value={riskTolerance} onChange={(e) => setRiskTolerance(e.target.value as RiskTolerance)}>
            <option value="unspecified">unspecified</option>
            <option value="low">low</option>
            <option value="medium">medium</option>
            <option value="high">high</option>
          </select>
        </label>
        <label>
          Technology preferences (comma-separated)
          <input value={techPrefs} onChange={(e) => setTechPrefs(e.target.value)} placeholder="e.g., .NET, Azure OpenAI" />
        </label>
      </div>

      <button type="submit" disabled={props.disabled}>
        Convene Council
      </button>
    </form>
  )
}
