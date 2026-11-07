import { Field, Input, Muted, Row, Stack } from '../components/ui'
import type { PlayerProps, QuestionEditorProps } from './types'

const num = (e: React.ChangeEvent<HTMLInputElement>) => (e.target.value === '' ? undefined : Number(e.target.value))

export function NumericEditor({ data, onChange }: QuestionEditorProps) {
  return (
    <Row>
      <Field label="Correct value">
        <Input type="number" step="any" value={typeof data.answer === 'number' ? data.answer : ''} onChange={(e) => onChange({ ...data, answer: num(e) })} />
      </Field>
      <Field label="Accepted tolerance (±)">
        <Input type="number" step="any" min={0} value={typeof data.tolerance === 'number' ? data.tolerance : ''} onChange={(e) => onChange({ ...data, tolerance: num(e) })} />
      </Field>
    </Row>
  )
}

export function NumericPlayer({ response, onChange, disabled, answerKey }: PlayerProps) {
  return (
    <Stack $gap={8}>
      <Field label="Your answer (a number)">
        <Input type="number" step="any" disabled={disabled} value={typeof response?.value === 'number' ? response.value : ''} onChange={(e) => onChange({ value: num(e) })} />
      </Field>
      {answerKey && (
        <Muted>
          Correct value: {String(answerKey.answer)}
          {typeof answerKey.tolerance === 'number' && answerKey.tolerance > 0 ? ` (±${answerKey.tolerance})` : ''}
        </Muted>
      )}
    </Stack>
  )
}
