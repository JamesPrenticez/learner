import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { exerciseType } from '../exercises/registry'
import { Badge, Button, Card, ErrorText, Muted, Row, Stack } from '../components/ui'
import { useExerciseAttempt, useExerciseView, useSubmitExercise } from '../services/exercises'
import type { ExerciseAttempt } from '../types/exercise'

const toResult = (a: ExerciseAttempt) => ({ summary: a.summary, details: a.details, score: a.score, maxScore: a.maxScore })

/**
 * Runs any registered exercise component inside a lesson. The component's Player draws the activity and its feedback;
 * this wrapper loads the (redacted) configuration, submits, and handles retries. It knows nothing about any specific exercise.
 */
export function ExercisePlayer({ exerciseId }: { exerciseId: number }) {
  const { data: view, isPending, error } = useExerciseView(exerciseId)
  const submit = useSubmitExercise(exerciseId)
  const [attempt, setAttempt] = useState<ExerciseAttempt | null>(null)
  const [round, setRound] = useState(0) // remounts the Player on "Try again" so it starts clean

  if (isPending) return <Card>Loading exercise…</Card>
  if (error) return <Card><ErrorText>{error.message}</ErrorText></Card>

  const def = exerciseType(view.typeKey)
  return (
    <Card aria-label={view.title}>
      <Stack>
        <Row $justify="space-between">
          <Row><Badge>Exercise</Badge><h3 style={{ margin: 0 }}>{view.title}</h3></Row>
          <Muted>{view.attempts === 0 ? 'Not attempted' : `${view.attempts} attempt${view.attempts === 1 ? '' : 's'} · best ${view.bestPercent}%`}</Muted>
        </Row>
        {view.problem ? (
          <ErrorText>This exercise is not ready yet: {view.problem}</ErrorText>
        ) : !def ? (
          <ErrorText>This exercise type is not supported.</ErrorText>
        ) : (
          <>
            <def.Player
              key={round}
              config={view.config}
              result={attempt ? toResult(attempt) : undefined}
              disabled={!!attempt}
              submitting={submit.isPending}
              onSubmit={(s) => submit.mutate(s, { onSuccess: setAttempt })}
            />
            {submit.error && <ErrorText>{submit.error.message}</ErrorText>}
            {attempt && (
              <Row>
                <Button onClick={() => { setAttempt(null); setRound((r) => r + 1) }}>Try again</Button>
                <Link to={`/exercise-results/${attempt.id}`}>View this result later</Link>
              </Row>
            )}
          </>
        )}
      </Stack>
    </Card>
  )
}

/** A past exercise attempt, replayed read-only with its original configuration. */
export function ExerciseResultPage() {
  const id = Number(useParams().attemptId)
  const { data: attempt, isPending, error } = useExerciseAttempt(id)
  if (isPending) return <p>Loading…</p>
  if (error) return <ErrorText>This result is not available.</ErrorText>
  const def = exerciseType(attempt.typeKey)
  return (
    <Stack>
      <p><Link to="/results">← All results</Link></p>
      <Card>
        <h1>{attempt.title}</h1>
        <Muted>Submitted {new Date(attempt.submittedAt).toLocaleString()} · exercise version {attempt.configVersion}</Muted>
      </Card>
      {def ? <def.Player config={attempt.config} submission={attempt.submission} result={toResult(attempt)} disabled submitting={false} onSubmit={() => {}} /> : <p>This exercise type is not supported.</p>}
    </Stack>
  )
}
