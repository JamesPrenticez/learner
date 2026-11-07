import { useState } from 'react'
import styled from 'styled-components'
import { Badge, Button, Card, Field, Input, Muted, Row, Stack, Textarea } from '../components/ui'
import { theme } from '../styles/theme'
import type { ExerciseEditorProps, ExercisePlayerProps } from './types'

type Clause = { id: string; title: string; text: string }
type Issue = { clauseId: string; explanation: string; points: number }
type IssueResult = Issue & { found: boolean }

const list = <T,>(v: unknown): T[] => (Array.isArray(v) ? (v as T[]) : [])

export function ContractReviewEditor({ config, onChange }: ExerciseEditorProps) {
  const clauses = list<Clause>(config.clauses)
  const issues = list<Issue>(config.issues)
  const set = (nextClauses: Clause[], nextIssues: Issue[]) =>
    onChange({ ...config, clauses: nextClauses, issues: nextIssues.filter((i) => nextClauses.some((c) => c.id === i.clauseId)) })
  const nextId = () => `c${Math.max(0, ...clauses.map((c) => Number(c.id.slice(1)) || 0)) + 1}`

  return (
    <Stack>
      <Field label="Scenario (what the learner is told about the project)">
        <Textarea value={typeof config.scenario === 'string' ? config.scenario : ''} onChange={(e) => onChange({ ...config, scenario: e.target.value })} />
      </Field>
      <Field label="Instructions">
        <Textarea value={typeof config.instructions === 'string' ? config.instructions : ''} onChange={(e) => onChange({ ...config, instructions: e.target.value })} />
      </Field>
      <h3 style={{ margin: 0 }}>Contract clauses</h3>
      <Muted>Tick “contains a problem” on the clauses learners should catch, and explain why. Learners never see these marks until they submit.</Muted>
      {clauses.map((c, i) => {
        const issue = issues.find((x) => x.clauseId === c.id)
        const update = (patch: Partial<Clause>) => set(clauses.map((x) => (x.id === c.id ? { ...x, ...patch } : x)), issues)
        return (
          <Card key={c.id} aria-label={`Clause ${i + 1}`}>
            <Stack $gap={8}>
              <Field label="Clause title"><Input value={c.title} onChange={(e) => update({ title: e.target.value })} /></Field>
              <Field label="Clause text"><Textarea value={c.text} onChange={(e) => update({ text: e.target.value })} /></Field>
              <label>
                <input
                  type="checkbox"
                  checked={!!issue}
                  onChange={(e) => set(clauses, e.target.checked ? [...issues, { clauseId: c.id, explanation: '', points: 1 }] : issues.filter((x) => x.clauseId !== c.id))}
                /> This clause contains a problem
              </label>
              {issue && (
                <Row $align="flex-end">
                  <div style={{ flex: '1 1 320px' }}>
                    <Field label="Why it is a problem (shown after submitting)">
                      <Textarea value={issue.explanation} onChange={(e) => set(clauses, issues.map((x) => (x === issue ? { ...x, explanation: e.target.value } : x)))} />
                    </Field>
                  </div>
                  <Field label="Points">
                    <Input type="number" min={1} value={issue.points} onChange={(e) => set(clauses, issues.map((x) => (x === issue ? { ...x, points: Math.max(1, Number(e.target.value)) } : x)))} />
                  </Field>
                </Row>
              )}
              <div><Button type="button" $variant="danger" onClick={() => set(clauses.filter((x) => x.id !== c.id), issues)}>Remove clause</Button></div>
            </Stack>
          </Card>
        )
      })}
      <div><Button type="button" $variant="ghost" onClick={() => set([...clauses, { id: nextId(), title: '', text: '' }], issues)}>Add clause</Button></div>
    </Stack>
  )
}

const ClauseCard = styled(Card)<{ $tone?: 'right' | 'wrong' | 'missed' }>`
  border-color: ${(p) => (p.$tone === 'right' ? theme.color.success : p.$tone ? theme.color.danger : theme.color.border)};
  background: ${(p) => (p.$tone === 'right' ? theme.color.successSoft : p.$tone ? theme.color.dangerSoft : theme.color.surface)};
`

export function ContractReviewPlayer({ config, submission, result, disabled, submitting, onSubmit }: ExercisePlayerProps) {
  const clauses = list<Clause>(config.clauses)
  const [flags, setFlags] = useState<Record<string, string>>(() =>
    Object.fromEntries(list<{ clauseId: string; note: string }>(submission?.flags).map((f) => [f.clauseId, f.note ?? ''])))

  const issues = list<IssueResult>(result?.details.issues)
  const falseFlags = list<string>(result?.details.falseFlags)

  return (
    <Stack>
      {typeof config.scenario === 'string' && config.scenario && <p style={{ whiteSpace: 'pre-wrap' }}><strong>Scenario.</strong> {config.scenario}</p>}
      {typeof config.instructions === 'string' && config.instructions && <p style={{ whiteSpace: 'pre-wrap' }}>{config.instructions}</p>}

      {clauses.map((c) => {
        const flagged = c.id in flags
        const issue = issues.find((i) => i.clauseId === c.id)
        const tone = !result ? undefined : issue ? (issue.found ? 'right' : 'missed') : falseFlags.includes(c.id) ? 'wrong' : undefined
        return (
          <ClauseCard key={c.id} $tone={tone} as="article" aria-label={c.title || 'Clause'}>
            <Stack $gap={8}>
              <Row $justify="space-between">
                <strong>{c.title}</strong>
                <Button
                  type="button"
                  $variant={flagged ? 'danger' : 'ghost'}
                  aria-pressed={flagged}
                  disabled={disabled}
                  onClick={() => setFlags((f) => (c.id in f ? Object.fromEntries(Object.entries(f).filter(([k]) => k !== c.id)) : { ...f, [c.id]: '' }))}
                >
                  {flagged ? '⚑ Flagged as a problem' : 'Flag as a problem'}
                </Button>
              </Row>
              <p style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{c.text}</p>
              {flagged && (
                <Field label="Your note (optional)">
                  <Input disabled={disabled} value={flags[c.id]} onChange={(e) => setFlags((f) => ({ ...f, [c.id]: e.target.value }))} />
                </Field>
              )}
              {result && issue && (
                <div role="status">
                  <Badge $tone={issue.found ? 'success' : 'danger'}>{issue.found ? '✓ Issue found' : '✗ Missed issue'}</Badge> <Muted>{issue.points} {issue.points === 1 ? 'point' : 'points'}</Muted>
                  <p style={{ margin: '4px 0 0' }}>{issue.explanation}</p>
                </div>
              )}
              {result && !issue && falseFlags.includes(c.id) && <div role="status"><Badge $tone="danger">✗ No problem here</Badge> <Muted>−1 point</Muted></div>}
            </Stack>
          </ClauseCard>
        )
      })}

      {result ? (
        <Card role="status">
          <strong>{result.score} / {result.maxScore} points.</strong> {result.summary}
        </Card>
      ) : (
        <div>
          <Button disabled={disabled || submitting} onClick={() => onSubmit({ flags: Object.entries(flags).map(([clauseId, note]) => ({ clauseId, note })) })}>Submit review</Button>
        </div>
      )}
    </Stack>
  )
}
