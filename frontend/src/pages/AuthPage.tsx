import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router'
import { Button, Card, Container, ErrorText, Field, Input, Stack } from '../components/ui'
import { useMe, useSignIn } from '../services/auth'

export function AuthPage({ mode }: { mode: 'login' | 'register' }) {
  const isRegister = mode === 'register'
  const { data: me } = useMe()
  const signIn = useSignIn(mode)
  const navigate = useNavigate()
  const from = (useLocation().state as { from?: string } | null)?.from ?? '/'
  const [form, setForm] = useState({ displayName: '', email: '', password: '' })

  if (me) return <Navigate to={from} replace />

  return (
    <Container style={{ maxWidth: 420 }}>
      <Card>
        <h1>{isRegister ? 'Create your account' : 'Sign in'}</h1>
        <form
          onSubmit={(e) => {
            e.preventDefault()
            signIn.mutate(form, { onSuccess: () => navigate(from, { replace: true }) })
          }}
        >
          <Stack>
            {isRegister && (
              <Field label="Name">
                <Input required value={form.displayName} onChange={(e) => setForm({ ...form, displayName: e.target.value })} autoComplete="name" />
              </Field>
            )}
            <Field label="Email">
              <Input type="email" required value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} autoComplete="email" />
            </Field>
            <Field label="Password">
              <Input
                type="password"
                required
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
                autoComplete={isRegister ? 'new-password' : 'current-password'}
              />
            </Field>
            {isRegister && <small>At least 6 characters.</small>}
            {signIn.error && <ErrorText>{signIn.error.message}</ErrorText>}
            <Button type="submit" disabled={signIn.isPending}>{isRegister ? 'Register' : 'Sign in'}</Button>
            <span>
              {isRegister ? <Link to="/login">Already have an account? Sign in</Link> : <Link to="/register">New here? Create an account</Link>}
            </span>
          </Stack>
        </form>
      </Card>
    </Container>
  )
}
