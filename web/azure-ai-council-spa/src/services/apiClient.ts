import type { CreateMotionRequest, CreateMotionResponse, GetDeliberationResponse } from '../types'

const defaultBaseUrl = 'http://localhost:5139'

export function apiBaseUrl(): string {
  const fromEnv = import.meta.env.VITE_API_BASE_URL as string | undefined
  return (fromEnv && fromEnv.trim().length > 0 ? fromEnv : defaultBaseUrl).replace(/\/$/, '')
}

export async function createMotion(req: CreateMotionRequest): Promise<CreateMotionResponse> {
  const res = await fetch(`${apiBaseUrl()}/api/motions`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify(req),
  })

  if (!res.ok) {
    const text = await res.text()
    throw new Error(`POST /api/motions failed: ${res.status} ${text}`)
  }

  return res.json()
}

export async function getDeliberation(id: string): Promise<GetDeliberationResponse> {
  const res = await fetch(`${apiBaseUrl()}/api/deliberations/${encodeURIComponent(id)}`)
  if (!res.ok) {
    const text = await res.text()
    throw new Error(`GET /api/deliberations/${id} failed: ${res.status} ${text}`)
  }
  return res.json()
}
