import { Field, Muted, Select, Stack } from '../components/ui'
import { QuizPlayer } from '../learner/QuizPlayer'
import { useQuizzes } from '../services/assessment'
import { quizKindLabel } from '../types/assessment'
import type { EditorProps, ViewProps } from './types'

/** Shared by the "quiz" and "knowledgecheck" blocks: both embed a quiz from this course. */
export function QuizEditor({ data, onChange, courseId }: EditorProps) {
  const { data: quizzes = [] } = useQuizzes(courseId)
  return (
    <Stack $gap={8}>
      <Field label="Quiz">
        <Select value={typeof data.quizId === 'number' ? data.quizId : ''} onChange={(e) => onChange({ ...data, quizId: e.target.value ? Number(e.target.value) : undefined })}>
          <option value="">Select a quiz…</option>
          {quizzes.map((q) => <option key={q.id} value={q.id}>{q.title} ({quizKindLabel[q.kind]}, {q.questionIds.length} questions)</option>)}
        </Select>
      </Field>
      <Muted>Create and configure quizzes in the Quizzes tab.</Muted>
    </Stack>
  )
}

export function QuizView({ data }: ViewProps) {
  return typeof data.quizId === 'number' ? <QuizPlayer quizId={data.quizId} /> : <p><em>No quiz selected.</em></p>
}
