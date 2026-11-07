import { Navigate, useLocation } from 'react-router'
import { hasRole, useMe } from '../services/auth'
import type { Role } from '../types/auth'
import { Container, ErrorText } from './ui'

/** Redirects to /login when signed out; shows a message when signed in without one of `roles`. Server enforces the real check. */
export function RequireRole({ roles, children }: { roles?: Role[]; children: React.ReactNode }) {
  const { data: me, isPending } = useMe()
  const location = useLocation()

  if (isPending) return <Container>Loading…</Container>
  if (!me) return <Navigate to="/login" replace state={{ from: location.pathname }} />
  if (roles && !hasRole(me, ...roles)) {
    return (
      <Container>
        <ErrorText>You do not have access to this page.</ErrorText>
      </Container>
    )
  }
  return children
}
