import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { Me, Role } from '../types/auth'
import { api, ApiError } from './api'

/** The signed-in user, or null when signed out. */
export function useMe() {
  return useQuery({
    queryKey: ['me'],
    staleTime: Infinity,
    queryFn: () =>
      api<Me>('/api/auth/me').catch((e) => {
        if (e instanceof ApiError && e.status === 401) return null
        throw e
      }),
  })
}

export function useSignIn(kind: 'login' | 'register') {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: { email: string; password: string; displayName?: string }) => api<Me>(`/api/auth/${kind}`, { body }),
    onSuccess: (me) => {
      qc.clear()
      qc.setQueryData(['me'], me)
    },
  })
}

export function useSignOut() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () => api<void>('/api/auth/logout', { method: 'POST', body: {} }),
    onSuccess: () => {
      qc.clear()
      qc.setQueryData(['me'], null)
    },
  })
}

export const hasRole = (me: Me | null | undefined, ...roles: Role[]) => !!me && roles.some((r) => me.roles.includes(r))

export function useUsers() {
  return useQuery({ queryKey: ['admin', 'users'], queryFn: () => api<Me[]>('/api/admin/users') })
}

export function useSetRoles() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, roles }: { id: string; roles: Role[] }) => api<Me>(`/api/admin/users/${id}/roles`, { method: 'PUT', body: { roles } }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin', 'users'] }),
  })
}
