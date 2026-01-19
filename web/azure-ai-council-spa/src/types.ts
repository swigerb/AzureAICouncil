export type RiskTolerance = 'low' | 'medium' | 'high' | 'unspecified'
export type Tier = 'low' | 'medium' | 'high' | 'unknown'
export type GoNoGo = 'go' | 'no_go' | 'mixed'
export type Priority = 'low' | 'medium' | 'high'
export type Councilor = 'Vision' | 'Risk' | 'Architecture' | 'Security' | 'Finance'

export interface MotionConstraints {
  budget?: string | null
  timeline?: string | null
  riskTolerance?: RiskTolerance
  technologyPreferences?: string[]
}

export interface Motion {
  motionId: string
  question: string
  constraints: MotionConstraints
  createdAt: string
}

export interface CouncilorStatement {
  councilor: Councilor
  round: number
  stanceTitle: string
  answer: string
  rationale: string
  risksOrConcerns: string[]
  costTier: Tier
  complexityTier: Tier
  confidence: number
  position: { goNoGo: GoNoGo; priority: Priority }
  notesForCouncil: string[]
  mermaidHint?: string | null
  createdAt: string
}

export interface CouncilResolution {
  deliberationId: string
  summary: string
  recommendation: string
  pros: string[]
  cons: string[]
  scores: { risk: Tier; cost: Tier; complexity: Tier; confidence: number }
  whenThisIsAGoodIdea: string[]
  whenToAvoid: string[]
  suggestedNextSteps: string[]
  mermaidArchitectureSnippet: string
  createdAt: string
}

export interface CreateMotionRequest {
  question: string
  constraints?: {
    budget?: string
    timeline?: string
    riskTolerance?: RiskTolerance
    technologyPreferences?: string[]
  }
}

export interface CreateMotionResponse {
  deliberationId: string
  motion: Motion
  resolution?: CouncilResolution | null
}

export interface DeliberationRoundView {
  round: number
  statements: CouncilorStatement[]
}

export interface GetDeliberationResponse {
  deliberationId: string
  motion: Motion
  status: string
  rounds: DeliberationRoundView[]
  resolution?: CouncilResolution | null
}

export type StreamEventName =
  | 'stream.ready'
  | 'session.started'
  | 'round.started'
  | 'councilor.status'
  | 'statement'
  | 'round.completed'
  | 'resolution'
  | 'error'

export interface CouncilorStatusUpdate {
  round: number
  councilor: Councilor
  state: string
}

export interface RoundStartedUpdate {
  round: number
  goal: string
}

export interface ErrorUpdate {
  where: string
  message: string
  retryable: boolean
}
