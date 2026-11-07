import { useId } from 'react'
import styled from 'styled-components'
import { Button, Input, Muted, Row, Stack } from '../components/ui'
import { theme } from '../styles/theme'
import type { PlayerProps, QuestionEditorProps } from './types'

type Option = { id: string; text: string }

const options = (data: Record<string, unknown>) => (Array.isArray(data.options) ? (data.options as Option[]) : [])
const correctIds = (data: Record<string, unknown>, multiple: boolean): string[] =>
  multiple ? (Array.isArray(data.correct) ? (data.correct as string[]) : []) : typeof data.correct === 'string' && data.correct ? [data.correct] : []

/** First unused a, b, c… so ids stay stable when options are removed. */
const nextId = (opts: Option[]) => Array.from({ length: 26 }, (_, i) => String.fromCharCode(97 + i)).find((c) => !opts.some((o) => o.id === c)) ?? crypto.randomUUID()

function ChoiceEditor({ data, onChange, multiple }: QuestionEditorProps & { multiple: boolean }) {
  const opts = options(data)
  const correct = correctIds(data, multiple)
  const set = (nextOpts: Option[], nextCorrect: string[]) =>
    onChange({ ...data, options: nextOpts, correct: multiple ? nextCorrect : (nextCorrect[0] ?? '') })
  const group = useId()

  return (
    <Stack $gap={8}>
      <Muted>{multiple ? 'Tick every correct option.' : 'Select the one correct option.'}</Muted>
      {opts.map((o, i) => (
        <Row key={o.id} $wrap={false}>
          <input
            type={multiple ? 'checkbox' : 'radio'}
            name={group}
            aria-label={`Option ${i + 1} is correct`}
            checked={correct.includes(o.id)}
            onChange={(e) => set(opts, multiple ? (e.target.checked ? [...correct, o.id] : correct.filter((c) => c !== o.id)) : [o.id])}
          />
          <Input aria-label={`Option ${i + 1} text`} value={o.text} onChange={(e) => set(opts.map((x) => (x.id === o.id ? { ...x, text: e.target.value } : x)), correct)} />
          <Button type="button" $variant="ghost" aria-label={`Remove option ${i + 1}`} onClick={() => set(opts.filter((x) => x.id !== o.id), correct.filter((c) => c !== o.id))}>✕</Button>
        </Row>
      ))}
      <div><Button type="button" $variant="ghost" onClick={() => set([...opts, { id: nextId(opts), text: '' }], correct)}>Add option</Button></div>
    </Stack>
  )
}

const Choice = styled.label<{ $tone?: 'right' | 'wrong' }>`
  display: flex;
  gap: 8px;
  align-items: flex-start;
  padding: 8px 10px;
  border: 1px solid ${(p) => (p.$tone === 'right' ? theme.color.success : p.$tone === 'wrong' ? theme.color.danger : theme.color.border)};
  background: ${(p) => (p.$tone === 'right' ? theme.color.successSoft : p.$tone === 'wrong' ? theme.color.dangerSoft : theme.color.surface)};
  border-radius: ${theme.radius};
`

function ChoicePlayer({ data, response, onChange, disabled, answerKey, multiple }: PlayerProps & { multiple: boolean }) {
  const group = useId()
  const opts = options(data)
  const selected: string[] = multiple ? (Array.isArray(response?.choices) ? (response.choices as string[]) : []) : typeof response?.choice === 'string' ? [response.choice] : []
  const correct = answerKey ? correctIds(answerKey, multiple) : undefined

  return (
    <fieldset disabled={disabled} style={{ border: 0, padding: 0, margin: 0 }}>
      <legend className="sr-only">{multiple ? 'Choose all that apply' : 'Choose one answer'}</legend>
      <Stack $gap={8}>
        {opts.map((o) => {
          const isSelected = selected.includes(o.id)
          const tone = correct ? (correct.includes(o.id) ? 'right' : isSelected ? 'wrong' : undefined) : undefined
          return (
            <Choice key={o.id} $tone={tone}>
              <input
                type={multiple ? 'checkbox' : 'radio'}
                name={group}
                checked={isSelected}
                onChange={(e) => onChange(multiple ? { choices: e.target.checked ? [...selected, o.id] : selected.filter((s) => s !== o.id) } : { choice: o.id })}
              />
              <span>
                {o.text}
                {tone === 'right' && <strong> ✓ Correct answer</strong>}
                {tone === 'wrong' && <strong> ✗ Your answer</strong>}
              </span>
            </Choice>
          )
        })}
      </Stack>
    </fieldset>
  )
}

export const SingleEditor = (p: QuestionEditorProps) => <ChoiceEditor {...p} multiple={false} />
export const MultipleEditor = (p: QuestionEditorProps) => <ChoiceEditor {...p} multiple />
export const SinglePlayer = (p: PlayerProps) => <ChoicePlayer {...p} multiple={false} />
export const MultiplePlayer = (p: PlayerProps) => <ChoicePlayer {...p} multiple />
