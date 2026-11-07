import { Field, Input, Muted, Stack } from '../components/ui'
import type { PlayerProps, QuestionEditorProps } from './types'

const accepted = (data: Record<string, unknown>) => (Array.isArray(data.accepted) ? (data.accepted as string[]) : [])

export function FillEditor({ data, onChange }: QuestionEditorProps) {
  return (
    <Stack $gap={8}>
      <Field label="Accepted answers (separate alternatives with a | )">
        <Input value={accepted(data).join(' | ')} onChange={(e) => onChange({ ...data, accepted: e.target.value.split('|').map((s) => s.trim()).filter(Boolean) })} />
      </Field>
      <label>
        <input type="checkbox" checked={data.caseSensitive === true} onChange={(e) => onChange({ ...data, caseSensitive: e.target.checked })} /> Case sensitive
      </label>
    </Stack>
  )
}

export function FillPlayer({ response, onChange, disabled, answerKey }: PlayerProps) {
  return (
    <Stack $gap={8}>
      <Field label="Your answer">
        <Input disabled={disabled} value={typeof response?.text === 'string' ? response.text : ''} onChange={(e) => onChange({ text: e.target.value })} autoComplete="off" />
      </Field>
      {answerKey && <Muted>Accepted answer{accepted(answerKey).length > 1 ? 's' : ''}: {accepted(answerKey).join(', ')}</Muted>}
    </Stack>
  )
}
