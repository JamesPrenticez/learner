import type { User } from '../types/user'

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:3001'

export async function fetchUser(): Promise<User> {
  const res = await fetch(`${API_URL}/api/user`)
  if (!res.ok) throw new Error(`GET /api/user failed: ${res.status}`)
  return res.json()
}
