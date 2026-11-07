import { AddForm, InlineEdit } from '../components/forms'
import { Button, Card, ErrorText, Muted, Row, Stack } from '../components/ui'
import { useCompetencies, useCompetencyActions } from '../services/assessment'
import { useCourse } from './useCourse'

/** The course's competency framework: the skills that questions, lessons and exercises provide evidence for. */
export function CompetenciesTab() {
  const course = useCourse()
  const { data: competencies, isPending, error } = useCompetencies(course.id)
  const actions = useCompetencyActions(course.id)
  const failure = error ?? actions.add.error ?? actions.update.error ?? actions.remove.error

  return (
    <Card>
      <h2>Competencies</h2>
      <p><Muted>Define what learners should be able to do. Then tag questions, lessons and exercises with them; evidence builds up automatically.</Muted></p>
      {failure && <ErrorText>{failure.message}</ErrorText>}
      {isPending && <p>Loading…</p>}
      <Stack $gap={8}>
        {competencies?.map((c) => (
          <Row key={c.id} $justify="space-between">
            <InlineEdit value={c.name} onSave={(name) => actions.update.mutate({ ...c, name })} />
            <Button $variant="danger" aria-label={`Delete ${c.name}`} onClick={() => confirm(`Delete competency "${c.name}"?`) && actions.remove.mutate(c.id)}>Delete</Button>
          </Row>
        ))}
        {competencies?.length === 0 && <p>No competencies yet.</p>}
        <AddForm label="New competency, e.g. Variation assessment" onAdd={(name) => actions.add.mutate({ name })} />
      </Stack>
    </Card>
  )
}
