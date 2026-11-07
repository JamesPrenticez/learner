import { useState } from 'react'
import { AddForm } from '../components/forms'
import { Badge, Button, Card, ErrorText, Field, Input, Muted, Row, Select, Stack } from '../components/ui'
import { exerciseType, exerciseTypes } from '../exercises/registry'
import { useCompetencies } from '../services/assessment'
import { useExerciseActions, useExerciseDetail, useExercises } from '../services/exercises'
import type { ExerciseConfig, ExerciseDetail } from '../types/exercise'
import { useCourse } from './useCourse'

/** Authoring form for any exercise type. The fields come from the selected component's own Editor. */
function ExerciseForm({ exercise, courseId, onDone }: { exercise: ExerciseDetail; courseId: number; onDone: () => void }) {
  const def = exerciseType(exercise.typeKey)
  const [title, setTitle] = useState(exercise.title)
  const [config, setConfig] = useState<ExerciseConfig>(Object.keys(exercise.config).length ? exercise.config : (def?.defaults ?? {}))
  const [competencyIds, setCompetencyIds] = useState(exercise.competencyIds)
  const { data: competencies = [] } = useCompetencies(courseId)
  const { update } = useExerciseActions(courseId)

  if (!def) return <ErrorText>This exercise type is not supported by this version of the app.</ErrorText>
  return (
    <Card aria-label={`Edit ${exercise.title}`}>
      <form
        onSubmit={(e) => {
          e.preventDefault()
          update.mutate({ id: exercise.id, title, config, competencyIds }, { onSuccess: onDone })
        }}
      >
        <Stack>
          <Row $align="flex-end"><Field label="Title"><Input required value={title} onChange={(e) => setTitle(e.target.value)} /></Field><Badge>{def.label}</Badge></Row>
          <def.Editor config={config} onChange={setConfig} />
          <fieldset style={{ border: '1px solid #d5d9e2', borderRadius: 8 }}>
            <legend>Competencies this exercise provides evidence for</legend>
            {competencies.length === 0 && <Muted>Add competencies in the Competencies tab first.</Muted>}
            <Row>
              {competencies.map((c) => (
                <label key={c.id}>
                  <input type="checkbox" checked={competencyIds.includes(c.id)} onChange={(e) => setCompetencyIds(e.target.checked ? [...competencyIds, c.id] : competencyIds.filter((x) => x !== c.id))} /> {c.name}
                </label>
              ))}
            </Row>
          </fieldset>
          {update.error && <ErrorText>{update.error.message}</ErrorText>}
          <Row>
            <Button type="submit" disabled={update.isPending}>Save exercise</Button>
            <Button type="button" $variant="ghost" onClick={onDone}>Cancel</Button>
          </Row>
        </Stack>
      </form>
    </Card>
  )
}

function ExerciseEditor({ id, courseId, onDone }: { id: number; courseId: number; onDone: () => void }) {
  const { data, isPending, error } = useExerciseDetail(id)
  if (isPending) return <p>Loading…</p>
  if (error) return <ErrorText>{error.message}</ErrorText>
  return <ExerciseForm key={`${data.id}-${data.version}`} exercise={data} courseId={courseId} onDone={onDone} />
}

export function ExercisesTab() {
  const course = useCourse()
  const { data: exercises = [] } = useExercises(course.id)
  const actions = useExerciseActions(course.id)
  const [typeKey, setTypeKey] = useState(exerciseTypes[0].type)
  const [editingId, setEditingId] = useState<number | null>(null)
  const failure = actions.create.error ?? actions.remove.error

  return (
    <Stack $gap={16}>
      <Card>
        <h2>Exercises and simulations</h2>
        <p><Muted>Choose an exercise type, configure its scenario, then embed it in a lesson with an Exercise block.</Muted></p>
        <Row $align="flex-end" $wrap={false}>
          <Select aria-label="Exercise type" value={typeKey} onChange={(e) => setTypeKey(e.target.value)} style={{ width: 'auto' }}>
            {exerciseTypes.map((t) => <option key={t.type} value={t.type}>{t.label}</option>)}
          </Select>
          <div style={{ flex: 1 }}>
            <AddForm label="Title" button="Create" onAdd={(title) => actions.create.mutate({ typeKey, title }, { onSuccess: (e) => setEditingId(e.id) })} />
          </div>
        </Row>
        {failure && <ErrorText>{failure.message}</ErrorText>}
      </Card>

      {editingId !== null && <ExerciseEditor id={editingId} courseId={course.id} onDone={() => setEditingId(null)} />}

      {exercises.map((x) => (
        <Card key={x.id}>
          <Row $justify="space-between">
            <Stack $gap={4}>
              <strong>{x.title}</strong>
              <Row>
                <Badge>{exerciseType(x.typeKey)?.label ?? x.typeKey}</Badge>
                <Muted>version {x.version}</Muted>
                {x.problem ? <Badge $tone="warn">Needs attention: {x.problem}</Badge> : <Badge $tone="success">Ready</Badge>}
              </Row>
            </Stack>
            <Row>
              <Button $variant="ghost" onClick={() => setEditingId(x.id)}>Edit</Button>
              <Button $variant="danger" aria-label={`Delete ${x.title}`} onClick={() => confirm(`Delete "${x.title}"?`) && actions.remove.mutate(x.id)}>Delete</Button>
            </Row>
          </Row>
        </Card>
      ))}
    </Stack>
  )
}
