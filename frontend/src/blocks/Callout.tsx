import styled from 'styled-components'
import { Field, Select, Stack, Textarea } from '../components/ui'
import { theme } from '../styles/theme'
import { str, type EditorProps, type ViewProps } from './types'

const TONES = {
  info: { bg: theme.color.primarySoft, fg: theme.color.primary, label: 'Important' },
  warning: { bg: theme.color.warnSoft, fg: theme.color.warn, label: 'Warning' },
  tip: { bg: theme.color.successSoft, fg: theme.color.success, label: 'Tip' },
} as const

const Box = styled.div<{ $variant: keyof typeof TONES }>`
  background: ${(p) => TONES[p.$variant].bg};
  border-left: 4px solid ${(p) => TONES[p.$variant].fg};
  border-radius: ${theme.radius};
  padding: 12px 16px;
  strong { color: ${(p) => TONES[p.$variant].fg}; }
  p:last-child { margin-bottom: 0; }
`

const variantOf = (data: Record<string, unknown>): keyof typeof TONES => (str(data, 'variant') in TONES ? (str(data, 'variant') as keyof typeof TONES) : 'info')

export function CalloutEditor({ data, onChange }: EditorProps) {
  return (
    <Stack>
      <Field label="Type">
        <Select value={variantOf(data)} onChange={(e) => onChange({ ...data, variant: e.target.value })}>
          <option value="info">Important information</option>
          <option value="warning">Warning</option>
          <option value="tip">Tip</option>
        </Select>
      </Field>
      <Field label="Text"><Textarea value={str(data, 'text')} onChange={(e) => onChange({ ...data, text: e.target.value })} /></Field>
    </Stack>
  )
}

export function CalloutView({ data }: ViewProps) {
  const v = variantOf(data)
  return (
    <Box $variant={v} role="note">
      <strong>{TONES[v].label}</strong>
      <p style={{ whiteSpace: 'pre-wrap' }}>{str(data, 'text')}</p>
    </Box>
  )
}
