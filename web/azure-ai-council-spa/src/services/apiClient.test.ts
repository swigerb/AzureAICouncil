import { describe, expect, it, vi } from 'vitest'
import { createMotion } from './apiClient'

describe('apiClient', () => {
  it('createMotion calls the API and returns parsed JSON', async () => {
    const fetchMock = vi.fn(async () => {
      return new Response(JSON.stringify({ deliberationId: 'abc123', motion: { motionId: 'm1', question: 'q' } }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    })

    vi.stubGlobal('fetch', fetchMock as unknown as typeof fetch)

    const result = await createMotion({ question: 'q' })

    expect(fetchMock).toHaveBeenCalled()
    expect(String(fetchMock.mock.calls[0]?.[0] ?? '')).toMatch(/\/api\/motions$/)
    expect(result.deliberationId).toBe('abc123')
  })
})
