import { useState } from 'react'
import { Link } from 'react-router'
import { Badge, Card, ErrorText, Muted, ProgressBar, Row, Stack } from '../components/ui'
import { useEvidence, useMyCompetencies } from '../services/competencies'
import { competencyStateLabel, competencyStateTone, type CompetencyStatus } from '../types/competency'

function Evidence({ id }: { id: number }) {
  const { data, isPending, error } = useEvidence(id, true)
  if (isPending) return <p>Loading…</p>
  if (error) return <ErrorText>{error.message}</ErrorText>
  return (
    <ul>
      {data.map((e) => (
        <li key={e.id}>
          {new Date(e.at).toLocaleDateString()} · <Link to={e.source === 'exercise' ? `/exercise-results/${e.sourceId}` : `/results/${e.sourceId}`}>{e.title}</Link> · {e.percent}% <Muted>(weight {e.weight})</Muted>
        </li>
      ))}
    </ul>
  )
}

function CompetencyCard({ c }: { c: CompetencyStatus }) {
  const [open, setOpen] = useState(false)
  return (
    <Card as="article">
      <Stack $gap={8}>
        <Row $justify="space-between">
          <h3 style={{ margin: 0 }}>{c.name}</h3>
          <Badge $tone={competencyStateTone[c.state]}>{competencyStateLabel[c.state]}</Badge>
        </Row>
        {c.evidence > 0 && <ProgressBar $pct={c.percent} role="progressbar" aria-label={`${c.name} score`} aria-valuenow={c.percent} aria-valuemin={0} aria-valuemax={100} />}
        <Muted>{c.evidence === 0 ? 'No assessment evidence yet. Try a quiz in this course.' : `${c.percent}% across recent results · ${c.evidence} assessment${c.evidence === 1 ? '' : 's'}`}</Muted>
        {c.recommended.length > 0 && (
          <div>
            <strong>Recommended:</strong>{' '}
            {c.recommended.map((l, i) => (
              <span key={l.id}>
                {i > 0 && ', '}
                <Link to={`/courses/${l.courseId}/lessons/${l.id}`}>{l.title}</Link>{l.completed && ' (revisit)'}
              </span>
            ))}
          </div>
        )}
        {c.evidence > 0 && (
          <details onToggle={(e) => setOpen(e.currentTarget.open)}>
            <summary>Show the evidence</summary>
            {open && <Evidence id={c.id} />}
          </details>
        )}
      </Stack>
    </Card>
  )
}

export function CompetenciesPage() {
  const { data: competencies, isPending, error } = useMyCompetencies()
  if (isPending) return <p>Loading…</p>
  if (error) return <ErrorText>{error.message}</ErrorText>

  const courses = [...new Set(competencies.map((c) => c.courseTitle))]
  return (
    <Stack $gap={16}>
      <h1>My competencies</h1>
      <p><Muted>Competency comes from what you demonstrate in assessments, not from viewing lessons. It updates as you complete more.</Muted></p>
      {competencies.length === 0 && <p>Enrol in a course that defines competencies to see them here.</p>}
      {courses.map((title) => (
        <section key={title} aria-label={title}>
          <h2>{title}</h2>
          <Stack>{competencies.filter((c) => c.courseTitle === title).map((c) => <CompetencyCard key={c.id} c={c} />)}</Stack>
        </section>
      ))}
    </Stack>
  )
}
