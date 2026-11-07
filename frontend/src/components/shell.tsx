import { NavLink } from 'react-router'
import styled from 'styled-components'
import { useMe, useSignOut } from '../services/auth'
import { theme } from '../styles/theme'
import { Button, Container, Row } from './ui'

// Header chrome shared by the learner app and the course builder. `$tone` tells them apart at a glance.
const Bar = styled.header<{ $tone: 'learner' | 'builder' }>`
  background: ${(p) => (p.$tone === 'builder' ? theme.color.text : theme.color.surface)};
  color: ${(p) => (p.$tone === 'builder' ? theme.color.surface : theme.color.text)};
  border-bottom: 1px solid ${theme.color.border};
`

export const Brand = styled(NavLink)`
  font-weight: 700;
  font-size: 1.2rem;
  color: inherit;
  text-decoration: none;
`

export const TopLink = styled(NavLink)`
  color: inherit;
  opacity: 0.75;
  text-decoration: none;
  padding: 4px 2px;
  &.active { opacity: 1; border-bottom: 2px solid ${theme.color.primary}; }
`

export function Shell({ tone, brand, to, links, children }: {
  tone: 'learner' | 'builder'
  brand: string
  to: string
  links: React.ReactNode
  children: React.ReactNode
}) {
  const { data: me } = useMe()
  const signOut = useSignOut()
  return (
    <>
      <Bar $tone={tone}>
        <Container as="nav" aria-label="Main" style={{ paddingBlock: 12 }}>
          <Row $gap={20} $justify="space-between">
            <Row $gap={20}>
              <Brand to={to}>{brand}</Brand>
              {links}
            </Row>
            <Row>
              <span>{me?.displayName}</span>
              <Button $variant="ghost" onClick={() => signOut.mutate()}>Sign out</Button>
            </Row>
          </Row>
        </Container>
      </Bar>
      <main>
        <Container>{children}</Container>
      </main>
    </>
  )
}
