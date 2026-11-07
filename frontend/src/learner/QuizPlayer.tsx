import { useEffect, useState } from 'react'
import { Badge, Button, Card, ErrorText, Muted, Row, Stack } from '../components/ui'
import { api } from '../services/api'
import { useAttemptActions, useQuizInfo } from '../services/assessment'
import { quizKindLabel, type AttemptView, type QuestionData } from '../types/assessment'
import { AttemptReview } from './AttemptReview'
import { QuestionBody } from './QuestionBody'

function Countdown({ deadline, onExpire }: { deadline: string; onExpire: () => void }) {
  const [left, setLeft] = useState(() => new Date(deadline).getTime() - Date.now())
  useEffect(() => {
    const t = setInterval(() => setLeft(new Date(deadline).getTime() - Date.now()), 1000)
    return () => clearInterval(t)
  }, [deadline])
  useEffect(() => {
    if (left <= 0) onExpire()
  }, [left, onExpire])
  const s = Math.max(0, Math.floor(left / 1000))
  return <Badge $tone={s < 60 ? 'danger' : 'info'} role="timer">⏱ {Math.floor(s / 60)}:{String(s % 60).padStart(2, '0')} left</Badge>
}

/**
 * Takes any quiz kind (knowledge check, quiz, module assessment, exam): intro → one question at a time → review.
 * Used by lesson blocks, so it only needs a quiz id.
 */
export function QuizPlayer({ quizId }: { quizId: number }) {
  const { data: info, isPending, error } = useQuizInfo(quizId)
  const actions = useAttemptActions(quizId)
  const [attempt, setAttempt] = useState<AttemptView | null>(null)
  const [index, setIndex] = useState(0)
  const [responses, setResponses] = useState<Record<number, QuestionData>>({})
  const [problem, setProblem] = useState<string | null>(null)

  if (isPending) return <Card>Loading quiz…</Card>
  if (error) return <Card><ErrorText>{error.message}</ErrorText></Card>

  const begin = (a: AttemptView) => {
    setAttempt(a)
    setResponses({})
    setProblem(null)
    setIndex(Math.max(0, a.questions.findIndex((q) => !q.answered)))
  }

  // ---- intro
  if (!attempt) {
    const label = info.inProgressAttemptId ? 'Resume attempt' : info.attemptsUsed > 0 ? 'Try again' : 'Start'
    return (
      <Card aria-label={info.title}>
        <Stack $gap={8}>
          <Row><Badge>{quizKindLabel[info.kind]}</Badge><h3 style={{ margin: 0 }}>{info.title}</h3></Row>
          {info.description && <p style={{ margin: 0 }}>{info.description}</p>}
          <Muted>
            {info.questions} questions · pass mark {info.passPercent}%
            {info.timeLimitMinutes ? ` · ${info.timeLimitMinutes} min limit` : ''}
            {info.maxAttempts ? ` · ${info.attemptsUsed} of ${info.maxAttempts} attempts used` : info.attemptsUsed ? ` · ${info.attemptsUsed} attempts so far` : ''}
            {info.bestPercent !== null ? ` · best ${info.bestPercent}%` : ''}
          </Muted>
          {info.closedReason && <ErrorText>{info.closedReason}</ErrorText>}
          {actions.start.error && <ErrorText>{actions.start.error.message}</ErrorText>}
          <div>
            <Button onClick={() => actions.start.mutate(undefined, { onSuccess: begin })} disabled={actions.start.isPending || (!!info.closedReason && !info.inProgressAttemptId)}>{label}</Button>
          </div>
        </Stack>
      </Card>
    )
  }

  // ---- finished: review
  if (attempt.finished) {
    return (
      <Stack>
        <AttemptReview attempt={attempt} />
        <div><Button onClick={() => setAttempt(null)}>Back to quiz</Button></div>
      </Stack>
    )
  }

  // ---- in progress
  const q = attempt.questions[index]
  const answeredCount = attempt.questions.filter((x) => x.answered).length
  const response = responses[q.id]
  const finish = () => actions.finish.mutate(attempt.id, { onSuccess: setAttempt })
  const reload = () => api<AttemptView>(`/api/attempts/${attempt.id}`).then(setAttempt)

  const submit = () =>
    actions.answer.mutate({ attemptId: attempt.id, questionId: q.id, response: response! }, {
      onSuccess: (r) => {
        setProblem(null)
        setAttempt({ ...attempt, questions: attempt.questions.map((x) => (x.id === q.id ? { ...x, answered: true, result: r.result } : x)) })
      },
      onError: (e) => setProblem(e.message),
    })

  return (
    <Card aria-label={attempt.title}>
      <Stack>
        <Row $justify="space-between">
          <strong>{attempt.title}</strong>
          <Row>
            <Muted>{answeredCount} of {attempt.questions.length} answered</Muted>
            {attempt.deadline && <Countdown deadline={attempt.deadline} onExpire={reload} />}
          </Row>
        </Row>
        <nav aria-label="Questions">
          <Row $gap={4}>
            {attempt.questions.map((x, i) => (
              <Button key={x.id} $variant={i === index ? 'primary' : 'ghost'} aria-current={i === index ? 'step' : undefined} aria-label={`Question ${i + 1}${x.answered ? ' (answered)' : ''}`} onClick={() => setIndex(i)}>
                {i + 1}{x.answered ? '✓' : ''}
              </Button>
            ))}
          </Row>
        </nav>

        <QuestionBody key={q.id} q={q} index={index} response={response} onChange={(r) => setResponses({ ...responses, [q.id]: r })} locked={q.answered} />
        {q.answered && !q.result && <Muted role="status">Answer saved. Feedback is shown when you finish.</Muted>}
        {problem && (
          <Stack $gap={8}>
            <ErrorText>{problem}</ErrorText>
            <div><Button $variant="ghost" onClick={reload}>See results</Button></div>
          </Stack>
        )}

        <Row $justify="space-between">
          <Row>
            {!q.answered && <Button onClick={submit} disabled={response === undefined || actions.answer.isPending}>Submit answer</Button>}
            {q.answered && index < attempt.questions.length - 1 && <Button onClick={() => setIndex(index + 1)}>Next question</Button>}
          </Row>
          <Button
            $variant={answeredCount === attempt.questions.length ? 'primary' : 'ghost'}
            disabled={actions.finish.isPending}
            onClick={() => (answeredCount === attempt.questions.length || confirm('Finish now? Unanswered questions score zero.')) && finish()}
          >
            Finish attempt
          </Button>
        </Row>
        {actions.finish.error && <ErrorText>{actions.finish.error.message}</ErrorText>}
      </Stack>
    </Card>
  )
}
