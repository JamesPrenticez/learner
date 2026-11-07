import { useId } from 'react'
import { Stack } from '../components/ui'
import type { PlayerProps, QuestionEditorProps } from './types'

export function TrueFalseEditor({ data, onChange }: QuestionEditorProps) {
  const group = useId()
  return (
    <fieldset style={{ border: 0, padding: 0, margin: 0 }}>
      <legend>The statement is…</legend>
      {[true, false].map((v) => (
        <label key={String(v)} style={{ marginRight: 16 }}>
          <input type="radio" name={group} checked={data.correct === v} onChange={() => onChange({ ...data, correct: v })} /> {v ? 'True' : 'False'}
        </label>
      ))}
    </fieldset>
  )
}

export function TrueFalsePlayer({ response, onChange, disabled, answerKey }: PlayerProps) {
  const group = useId()
  return (
    <fieldset disabled={disabled} style={{ border: 0, padding: 0, margin: 0 }}>
      <legend className="sr-only">True or false</legend>
      <Stack $gap={8}>
        {[true, false].map((v) => (
          <label key={String(v)}>
            <input type="radio" name={group} checked={response?.value === v} onChange={() => onChange({ value: v })} /> {v ? 'True' : 'False'}
            {answerKey?.correct === v && <strong> ✓ Correct answer</strong>}
            {answerKey && response?.value === v && answerKey.correct !== v && <strong> ✗ Your answer</strong>}
          </label>
        ))}
      </Stack>
    </fieldset>
  )
}
