import { Link, useParams } from 'react-router'
import { Badge, Card, ErrorText, Table } from '../components/ui'
import { useAttempt, useMyAttempts } from '../services/assessment'
import { useMyExerciseAttempts } from '../services/exercises'
import { quizKindLabel } from '../types/assessment'
import { AttemptReview } from './AttemptReview'

export function ResultsPage() {
  const { data: attempts, isPending, error } = useMyAttempts()
  const { data: exercises } = useMyExerciseAttempts()
  return (
    <Card>
      <h1>My assessment results</h1>
      {isPending && <p>Loading…</p>}
      {error && <ErrorText>{error.message}</ErrorText>}
      {attempts?.length === 0 && <p>You have not completed any assessments yet.</p>}
      {attempts && attempts.length > 0 && (
        <Table>
          <thead>
            <tr><th>Date</th><th>Assessment</th><th>Course</th><th>Score</th><th>Result</th><th><span className="sr-only">Review</span></th></tr>
          </thead>
          <tbody>
            {attempts.map((a) => (
              <tr key={a.id}>
                <td>{new Date(a.finishedAt ?? a.startedAt).toLocaleString()}</td>
                <td>{a.quizTitle} <Badge>{quizKindLabel[a.kind]}</Badge></td>
                <td><Link to={`/courses/${a.courseId}`}>{a.courseTitle}</Link></td>
                <td>{a.score} / {a.maxScore} ({a.percent}%)</td>
                <td><Badge $tone={a.passed ? 'success' : 'warn'}>{a.passed ? 'Passed' : 'Not passed'}</Badge></td>
                <td><Link to={`/results/${a.id}`}>Review</Link></td>
              </tr>
            ))}
          </tbody>
        </Table>
      )}

      <h2 style={{ marginTop: 24 }}>Exercise attempts</h2>
      {exercises?.length === 0 && <p>You have not completed any exercises yet.</p>}
      {exercises && exercises.length > 0 && (
        <Table>
          <thead>
            <tr><th>Date</th><th>Exercise</th><th>Course</th><th>Score</th><th><span className="sr-only">Review</span></th></tr>
          </thead>
          <tbody>
            {exercises.map((a) => (
              <tr key={a.id}>
                <td>{new Date(a.submittedAt).toLocaleString()}</td>
                <td>{a.title}</td>
                <td><Link to={`/courses/${a.courseId}`}>{a.courseTitle}</Link></td>
                <td>{a.score} / {a.maxScore} ({a.percent}%)</td>
                <td><Link to={`/exercise-results/${a.id}`}>Review</Link></td>
              </tr>
            ))}
          </tbody>
        </Table>
      )}
    </Card>
  )
}

export function AttemptPage() {
  const id = Number(useParams().attemptId)
  const { data: attempt, isPending, error } = useAttempt(id)
  if (isPending) return <p>Loading…</p>
  if (error) return <ErrorText>This result is not available.</ErrorText>
  return (
    <>
      <p><Link to="/results">← All results</Link></p>
      <AttemptReview attempt={attempt} />
    </>
  )
}
