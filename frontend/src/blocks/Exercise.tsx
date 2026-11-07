import { Field, Muted, Select, Stack } from '../components/ui'
import { ExercisePlayer } from '../learner/ExercisePlayer'
import { exerciseType } from '../exercises/registry'
import { useExercises } from '../services/exercises'
import type { EditorProps, ViewProps } from './types'

export function ExerciseBlockEditor({ data, onChange, courseId }: EditorProps) {
  const { data: exercises = [] } = useExercises(courseId)
  return (
    <Stack $gap={8}>
      <Field label="Exercise">
        <Select value={typeof data.exerciseId === 'number' ? data.exerciseId : ''} onChange={(e) => onChange({ ...data, exerciseId: e.target.value ? Number(e.target.value) : undefined })}>
          <option value="">Select an exercise…</option>
          {exercises.map((x) => (
            <option key={x.id} value={x.id}>{x.title} ({exerciseType(x.typeKey)?.label ?? x.typeKey}){x.problem ? ' – incomplete' : ''}</option>
          ))}
        </Select>
      </Field>
      <Muted>Create and configure exercises in the Exercises tab.</Muted>
    </Stack>
  )
}

export function ExerciseBlockView({ data }: ViewProps) {
  return typeof data.exerciseId === 'number' ? <ExercisePlayer exerciseId={data.exerciseId} /> : <p><em>No exercise selected.</em></p>
}
