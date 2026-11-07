import { Link } from 'react-router'
import styled, { css } from 'styled-components'
import { theme } from '../styles/theme'

// Shared styled primitives. `$`-prefixed props are transient (not forwarded to the DOM).

export const Container = styled.div`
  max-width: ${theme.maxWidth};
  margin: 0 auto;
  padding: 24px 16px;
`

export const Card = styled.section`
  background: ${theme.color.surface};
  border: 1px solid ${theme.color.border};
  border-radius: ${theme.radius};
  padding: 16px;
`

export const Stack = styled.div<{ $gap?: number }>`
  display: flex;
  flex-direction: column;
  gap: ${(p) => p.$gap ?? 12}px;
`

export const Row = styled.div<{ $gap?: number; $justify?: string; $align?: string; $wrap?: boolean }>`
  display: flex;
  flex-wrap: ${(p) => (p.$wrap === false ? 'nowrap' : 'wrap')};
  gap: ${(p) => p.$gap ?? 8}px;
  justify-content: ${(p) => p.$justify ?? 'flex-start'};
  align-items: ${(p) => p.$align ?? 'center'};
`

export const Grid = styled.div`
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 16px;
`

export const Muted = styled.span`
  color: ${theme.color.muted};
`

export const ErrorText = styled.p.attrs({ role: 'alert' })`
  color: ${theme.color.danger};
  margin: 0;
`

type Variant = 'primary' | 'ghost' | 'danger'

const buttonStyles = css<{ $variant?: Variant }>`
  display: inline-block;
  font: inherit;
  padding: 8px 14px;
  border-radius: ${theme.radius};
  border: 1px solid ${(p) => (p.$variant === 'danger' ? theme.color.danger : p.$variant === 'ghost' ? theme.color.border : theme.color.primary)};
  background: ${(p) => (p.$variant === 'primary' || !p.$variant ? theme.color.primary : theme.color.surface)};
  color: ${(p) => (p.$variant === 'primary' || !p.$variant ? theme.color.primaryText : p.$variant === 'danger' ? theme.color.danger : theme.color.text)};
  cursor: pointer;
  text-decoration: none;
  &:disabled { opacity: 0.5; cursor: not-allowed; }
`

export const Button = styled.button<{ $variant?: Variant }>`
  ${buttonStyles}
`

export const ButtonLink = styled(Link)<{ $variant?: Variant }>`
  ${buttonStyles}
`

const fieldStyles = css`
  font: inherit;
  width: 100%;
  padding: 8px 10px;
  border: 1px solid ${theme.color.border};
  border-radius: ${theme.radius};
  background: ${theme.color.surface};
  color: ${theme.color.text};
`

export const Input = styled.input`
  ${fieldStyles}
`
export const Textarea = styled.textarea`
  ${fieldStyles}
  min-height: 90px;
  resize: vertical;
`
export const Select = styled.select`
  ${fieldStyles}
`

const FieldLabel = styled.label`
  display: flex;
  flex-direction: column;
  gap: 4px;
  font-weight: 600;
  & > :not(span) { font-weight: 400; }
`

/** Label + control. The control must be the (only) child so the label wraps it for accessibility. */
export function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <FieldLabel>
      <span>{label}</span>
      {children}
    </FieldLabel>
  )
}

export const Badge = styled.span<{ $tone?: 'info' | 'success' | 'warn' | 'danger' }>`
  display: inline-block;
  font-size: 0.8rem;
  padding: 2px 8px;
  border-radius: 999px;
  background: ${(p) => ({ info: theme.color.primarySoft, success: theme.color.successSoft, warn: theme.color.warnSoft, danger: theme.color.dangerSoft })[p.$tone ?? 'info']};
  color: ${(p) => ({ info: theme.color.primary, success: theme.color.success, warn: theme.color.warn, danger: theme.color.danger })[p.$tone ?? 'info']};
`

export const Table = styled.table`
  width: 100%;
  border-collapse: collapse;
  th, td { text-align: left; padding: 8px; border-bottom: 1px solid ${theme.color.border}; vertical-align: top; }
`

export const ProgressBar = styled.div<{ $pct: number }>`
  height: 8px;
  border-radius: 999px;
  background: ${theme.color.border};
  overflow: hidden;
  &::after {
    content: '';
    display: block;
    height: 100%;
    width: ${(p) => Math.max(0, Math.min(100, p.$pct))}%;
    background: ${theme.color.success};
  }
`
