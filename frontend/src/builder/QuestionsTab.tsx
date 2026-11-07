import { useState } from 'react'
import { AddForm, InlineEdit } from '../components/forms'
import { Badge, Button, Card, ErrorText, Field, Input, Muted, Row, Select, Stack, Textarea } from '../components/ui'
import { mediaUrl } from '../services/api'
import { useBankActions, useBanks, useCompetencies, useQuestionActions, useQuestions } from '../services/assessment'
import { useUpload } from '../services/media'
import { questionType, questionTypes } from '../questions/registry'
import type { Difficulty } from '../types/course'
import type { Question, QuestionInput } from '../types/assessment'
import { useCourse } from './useCourse'

const blank = (type = 'single'): QuestionInput => ({
  type, text: '', data: { ...questionType(type)!.defaults }, explanation: '', hint: '', difficulty: 'Beginner', marks: 1,
  category: '', tags: [], imageMediaId: null, competencyIds: [],
})

function QuestionForm({ courseId, initial, existingId, bankId, onDone }: {
  courseId: number
  initial: QuestionInput
  existingId?: number
  bankId: number
  onDone: () => void
}) {
  const [q, setQ] = useState(initial)
  const [tags, setTags] = useState(initial.tags.join(', '))
  const { data: competencies = [] } = useCompetencies(courseId)
  const actions = useQuestionActions(courseId)
  const upload = useUpload()
  const def = questionType(q.type)!
  const saving = existingId ? actions.update : actions.create
  const set = <K extends keyof QuestionInput>(k: K, v: QuestionInput[K]) => setQ((p) => ({ ...p, [k]: v }))

  return (
    <Card aria-label={existingId ? 'Edit question' : 'New question'}>
      <form
        onSubmit={(e) => {
          e.preventDefault()
          const input = { ...q, tags: tags.split(',').map((t) => t.trim()).filter(Boolean) }
          if (existingId) actions.update.mutate({ id: existingId, input }, { onSuccess: onDone })
          else actions.create.mutate({ bankId, input }, { onSuccess: onDone })
        }}
      >
        <Stack>
          <Row $align="flex-end">
            <Field label="Question type">
              <Select value={q.type} disabled={!!existingId} onChange={(e) => setQ({ ...q, type: e.target.value, data: { ...questionType(e.target.value)!.defaults } })}>
                {questionTypes.map((t) => <option key={t.type} value={t.type}>{t.label}</option>)}
              </Select>
            </Field>
            <Field label="Marks"><Input type="number" min={1} max={100} value={q.marks} onChange={(e) => set('marks', Number(e.target.value))} /></Field>
            <Field label="Difficulty">
              <Select value={q.difficulty} onChange={(e) => set('difficulty', e.target.value as Difficulty)}>
                <option>Beginner</option><option>Intermediate</option><option>Advanced</option>
              </Select>
            </Field>
          </Row>
          <Field label="Question text"><Textarea required value={q.text} onChange={(e) => set('text', e.target.value)} /></Field>
          <Field label="Supporting image (optional)">
            <input type="file" accept="image/png,image/jpeg,image/gif,image/webp" onChange={(e) => { const f = e.target.files?.[0]; if (f) upload.mutate(f, { onSuccess: (m) => set('imageMediaId', m.id) }) }} />
          </Field>
          {upload.error && <ErrorText>{upload.error.message}</ErrorText>}
          {q.imageMediaId && <Row><img src={mediaUrl(q.imageMediaId)} alt="" style={{ maxHeight: 80 }} /><Button type="button" $variant="ghost" onClick={() => set('imageMediaId', null)}>Remove image</Button></Row>}
          <fieldset style={{ border: `1px solid #d5d9e2`, borderRadius: 8 }}>
            <legend>Answer</legend>
            <def.Editor data={q.data} onChange={(data) => set('data', data)} />
          </fieldset>
          <Field label="Explanation (shown after answering)"><Textarea value={q.explanation} onChange={(e) => set('explanation', e.target.value)} /></Field>
          <Field label="Hint (optional)"><Input value={q.hint} onChange={(e) => set('hint', e.target.value)} /></Field>
          <Row>
            <Field label="Category"><Input value={q.category} onChange={(e) => set('category', e.target.value)} /></Field>
            <Field label="Tags (comma separated)"><Input value={tags} onChange={(e) => setTags(e.target.value)} /></Field>
          </Row>
          <fieldset style={{ border: `1px solid #d5d9e2`, borderRadius: 8 }}>
            <legend>Competencies this question provides evidence for</legend>
            {competencies.length === 0 && <Muted>Add competencies in the Competencies tab first.</Muted>}
            <Row>
              {competencies.map((c) => (
                <label key={c.id}>
                  <input type="checkbox" checked={q.competencyIds.includes(c.id)} onChange={(e) => set('competencyIds', e.target.checked ? [...q.competencyIds, c.id] : q.competencyIds.filter((x) => x !== c.id))} /> {c.name}
                </label>
              ))}
            </Row>
          </fieldset>
          {saving.error && <ErrorText>{saving.error.message}</ErrorText>}
          <Row>
            <Button type="submit" disabled={saving.isPending}>Save question</Button>
            <Button type="button" $variant="ghost" onClick={onDone}>Cancel</Button>
          </Row>
        </Stack>
      </form>
    </Card>
  )
}

export function QuestionsTab() {
  const course = useCourse()
  const { data: banks = [] } = useBanks(course.id)
  const { data: questions = [] } = useQuestions(course.id)
  const { data: competencies = [] } = useCompetencies(course.id)
  const bankActions = useBankActions(course.id)
  const questionActions = useQuestionActions(course.id)
  const [bankId, setBankId] = useState<number | null>(null)
  const [editing, setEditing] = useState<Question | 'new' | null>(null)

  const bank = banks.find((b) => b.id === bankId) ?? banks[0]
  const inBank = questions.filter((q) => q.bankId === bank?.id)
  const failure = bankActions.add.error ?? bankActions.rename.error ?? bankActions.remove.error ?? questionActions.remove.error

  return (
    <Stack $gap={16}>
      <Card>
        <h2>Question banks</h2>
        {failure && <ErrorText>{failure.message}</ErrorText>}
        <Row>
          {banks.map((b) => (
            <Button key={b.id} $variant={b.id === bank?.id ? 'primary' : 'ghost'} onClick={() => { setBankId(b.id); setEditing(null) }}>{b.name} ({b.questions})</Button>
          ))}
        </Row>
        <div style={{ marginTop: 12 }}>
          <AddForm label="New bank name, e.g. Variations" button="Create bank" onAdd={(name) => bankActions.add.mutate(name, { onSuccess: (b) => setBankId((b as { id: number }).id) })} />
        </div>
      </Card>

      {bank && (
        <Card>
          <Row $justify="space-between">
            <InlineEdit value={bank.name} onSave={(name) => bankActions.rename.mutate({ id: bank.id, name })} />
            <Row>
              <Button onClick={() => setEditing('new')}>Add question</Button>
              <Button $variant="danger" onClick={() => confirm(`Delete bank "${bank.name}" and its ${bank.questions} questions? Learner history is kept.`) && bankActions.remove.mutate(bank.id)}>Delete bank</Button>
            </Row>
          </Row>

          {editing && (
            <div style={{ marginTop: 16 }}>
              <QuestionForm
                key={editing === 'new' ? 'new' : editing.id}
                courseId={course.id}
                bankId={bank.id}
                initial={editing === 'new' ? blank() : editing}
                existingId={editing === 'new' ? undefined : editing.id}
                onDone={() => setEditing(null)}
              />
            </div>
          )}

          <Stack $gap={8} style={{ marginTop: 16 }}>
            {inBank.length === 0 && <p>No questions in this bank yet.</p>}
            {inBank.map((q) => (
              <Row key={q.id} $justify="space-between" $align="flex-start">
                <div style={{ flex: '1 1 300px' }}>
                  <div>{q.text}</div>
                  <Row $gap={6}>
                    <Badge>{questionType(q.type)?.label ?? q.type}</Badge>
                    <Badge $tone="warn">{q.difficulty}</Badge>
                    <Muted>{q.marks} {q.marks === 1 ? 'mark' : 'marks'}</Muted>
                    {q.competencyIds.map((id) => <Badge key={id} $tone="success">{competencies.find((c) => c.id === id)?.name}</Badge>)}
                    {q.tags.map((t) => <Muted key={t}>#{t}</Muted>)}
                  </Row>
                </div>
                <Row>
                  <Button $variant="ghost" onClick={() => setEditing(q)}>Edit</Button>
                  <Button $variant="danger" aria-label={`Delete question: ${q.text}`} onClick={() => confirm('Delete this question? Past attempts keep their copy.') && questionActions.remove.mutate(q.id)}>Delete</Button>
                </Row>
              </Row>
            ))}
          </Stack>
        </Card>
      )}
    </Stack>
  )
}
