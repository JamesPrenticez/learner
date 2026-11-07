import { useState } from 'react'
import { Badge, Button, Card, ErrorText, Field, Input, Muted, Row, Select, Stack, Textarea } from '../components/ui'
import { useBanks, useQuestions, useQuizActions, useQuizzes } from '../services/assessment'
import { questionType } from '../questions/registry'
import { quizKindLabel, type Quiz, type QuizInput, type QuizKind } from '../types/assessment'
import { AddForm } from '../components/forms'
import { useCourse } from './useCourse'

const toLocal = (iso: string | null) => (iso ? new Date(iso).toISOString().slice(0, 16) : '')
const fromLocal = (v: string) => (v ? new Date(v).toISOString() : null)
const intOrNull = (v: string) => (v === '' ? null : Number(v))

function QuizForm({ quiz, courseId, onDone }: { quiz: Quiz; courseId: number; onDone: () => void }) {
  const [q, setQ] = useState<QuizInput>(quiz)
  const [filter, setFilter] = useState('')
  const { data: questions = [] } = useQuestions(courseId)
  const { data: banks = [] } = useBanks(courseId)
  const actions = useQuizActions(courseId)
  const set = <K extends keyof QuizInput>(k: K, v: QuizInput[K]) => setQ((p) => ({ ...p, [k]: v }))
  const shown = questions.filter((x) => x.text.toLowerCase().includes(filter.toLowerCase()))
  const toggle = (id: number, on: boolean) => set('questionIds', on ? [...q.questionIds, id] : q.questionIds.filter((x) => x !== id))

  return (
    <Card aria-label={`Edit ${quiz.title}`}>
      <form
        onSubmit={(e) => {
          e.preventDefault()
          actions.update.mutate({ id: quiz.id, input: q }, { onSuccess: onDone })
        }}
      >
        <Stack>
          <Row $align="flex-end">
            <Field label="Title"><Input required value={q.title} onChange={(e) => set('title', e.target.value)} /></Field>
            <Field label="Kind">
              <Select value={q.kind} onChange={(e) => set('kind', e.target.value as QuizKind)}>
                {Object.entries(quizKindLabel).map(([k, label]) => <option key={k} value={k}>{label}</option>)}
              </Select>
            </Field>
          </Row>
          <Field label="Description"><Textarea value={q.description} onChange={(e) => set('description', e.target.value)} /></Field>
          <Row>
            <Field label="Questions per attempt (blank = all)"><Input type="number" min={1} value={q.questionCount ?? ''} onChange={(e) => set('questionCount', intOrNull(e.target.value))} /></Field>
            <Field label="Pass mark (%)"><Input type="number" min={0} max={100} value={q.passPercent} onChange={(e) => set('passPercent', Number(e.target.value))} /></Field>
            <Field label="Time limit (minutes)"><Input type="number" min={1} value={q.timeLimitMinutes ?? ''} onChange={(e) => set('timeLimitMinutes', intOrNull(e.target.value))} /></Field>
            <Field label="Attempt limit (blank = unlimited)"><Input type="number" min={1} value={q.maxAttempts ?? ''} onChange={(e) => set('maxAttempts', intOrNull(e.target.value))} /></Field>
          </Row>
          <Row>
            <Field label="Available from"><Input type="datetime-local" value={toLocal(q.availableFrom)} onChange={(e) => set('availableFrom', fromLocal(e.target.value))} /></Field>
            <Field label="Available until"><Input type="datetime-local" value={toLocal(q.availableUntil)} onChange={(e) => set('availableUntil', fromLocal(e.target.value))} /></Field>
          </Row>
          <Row $gap={16}>
            <label><input type="checkbox" checked={q.shuffleQuestions} onChange={(e) => set('shuffleQuestions', e.target.checked)} /> Randomise question order</label>
            <label><input type="checkbox" checked={q.shuffleOptions} onChange={(e) => set('shuffleOptions', e.target.checked)} /> Randomise answer order</label>
            <label><input type="checkbox" checked={q.immediateFeedback} onChange={(e) => set('immediateFeedback', e.target.checked)} /> Feedback after each answer</label>
          </Row>

          <fieldset style={{ border: '1px solid #d5d9e2', borderRadius: 8 }}>
            <legend>Question pool ({q.questionIds.length} selected)</legend>
            <Input aria-label="Filter questions" placeholder="Filter questions…" value={filter} onChange={(e) => setFilter(e.target.value)} style={{ marginBottom: 8 }} />
            <Stack $gap={4} style={{ maxHeight: 260, overflow: 'auto' }}>
              {questions.length === 0 && <Muted>Add questions in the Questions tab first.</Muted>}
              {shown.map((x) => (
                <label key={x.id}>
                  <input type="checkbox" checked={q.questionIds.includes(x.id)} onChange={(e) => toggle(x.id, e.target.checked)} /> {x.text}{' '}
                  <Muted>({questionType(x.type)?.label}, {banks.find((b) => b.id === x.bankId)?.name})</Muted>
                </label>
              ))}
            </Stack>
          </fieldset>

          {actions.update.error && <ErrorText>{actions.update.error.message}</ErrorText>}
          <Row>
            <Button type="submit" disabled={actions.update.isPending}>Save quiz</Button>
            <Button type="button" $variant="ghost" onClick={onDone}>Cancel</Button>
          </Row>
        </Stack>
      </form>
    </Card>
  )
}

export function QuizzesTab() {
  const course = useCourse()
  const { data: quizzes = [] } = useQuizzes(course.id)
  const actions = useQuizActions(course.id)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [kind, setKind] = useState<QuizKind>('Quiz')
  const editing = quizzes.find((q) => q.id === editingId)
  const failure = actions.create.error ?? actions.remove.error

  return (
    <Stack $gap={16}>
      <Card>
        <h2>Quizzes, knowledge checks and exams</h2>
        <p><Muted>Configure an assessment here, then embed it in a lesson with a Quiz or Knowledge check block.</Muted></p>
        <Row $align="flex-end" $wrap={false}>
          <Select aria-label="Kind" value={kind} onChange={(e) => setKind(e.target.value as QuizKind)} style={{ width: 'auto' }}>
            {Object.entries(quizKindLabel).map(([k, label]) => <option key={k} value={k}>{label}</option>)}
          </Select>
          <div style={{ flex: 1 }}>
            <AddForm label="Title" button="Create" onAdd={(title) => actions.create.mutate({ title, kind }, { onSuccess: (q) => setEditingId((q as Quiz).id) })} />
          </div>
        </Row>
        {failure && <ErrorText>{failure.message}</ErrorText>}
      </Card>

      {editing && <QuizForm key={editing.id} quiz={editing} courseId={course.id} onDone={() => setEditingId(null)} />}

      {quizzes.map((q) => (
        <Card key={q.id}>
          <Row $justify="space-between">
            <Stack $gap={4}>
              <strong>{q.title}</strong>
              <Row>
                <Badge>{quizKindLabel[q.kind]}</Badge>
                <Muted>{q.questionIds.length} in pool · {q.questionCount ?? 'all'} per attempt · pass {q.passPercent}%{q.timeLimitMinutes ? ` · ${q.timeLimitMinutes} min` : ''}{q.maxAttempts ? ` · ${q.maxAttempts} attempts` : ''}</Muted>
              </Row>
            </Stack>
            <Row>
              <Button $variant="ghost" onClick={() => setEditingId(q.id)}>Edit</Button>
              <Button $variant="danger" aria-label={`Delete ${q.title}`} onClick={() => confirm(`Delete "${q.title}"?`) && actions.remove.mutate(q.id)}>Delete</Button>
            </Row>
          </Row>
        </Card>
      ))}
    </Stack>
  )
}
