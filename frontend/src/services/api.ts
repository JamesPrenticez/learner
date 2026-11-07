const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:3001'

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

type Options = { method?: string; body?: unknown }

/** JSON fetch with the auth cookie. FormData bodies are sent as-is (uploads). */
export async function api<T>(path: string, { method, body }: Options = {}): Promise<T> {
  const isForm = body instanceof FormData
  const res = await fetch(`${API_URL}${path}`, {
    method: method ?? (body === undefined ? 'GET' : 'POST'),
    credentials: 'include',
    headers: body === undefined || isForm ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : isForm ? body : JSON.stringify(body),
  })
  if (!res.ok) throw new ApiError(res.status, await errorMessage(res))
  if (res.status === 204) return undefined as T
  return res.json()
}

async function errorMessage(res: Response): Promise<string> {
  try {
    const p = await res.json()
    if (p.errors) return (Object.values(p.errors) as string[][]).flat().join(' ')
    if (p.detail || p.title) return p.detail ?? p.title
  } catch {
    // not JSON
  }
  return res.status === 401 ? 'Please sign in.' : res.status === 403 ? 'You do not have permission.' : `Request failed (${res.status})`
}

/** URL for an uploaded media asset (images, documents). */
export const mediaUrl = (id: string) => `${API_URL}/api/media/${id}`
