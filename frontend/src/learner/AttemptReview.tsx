import { Badge, Card, Muted, Row, Stack } from '../components/ui'
import type { AttemptView } from '../types/assessment'
import { QuestionBody } from './QuestionBody'

/** A finished attempt: the score, then every question with the learner's answer, the correct answer and the explanation. */
export function AttemptReview({ attempt }: { attempt: AttemptView }) {
  return (
    <Stack $gap={16}>
      <Card aria-label="Result">
        <Row $justify="space-between">
          <div>
            <h2 style={{ margin: 0 }}>{attempt.title}</h2>
            <p style={{ margin: '4px 0 0' }}>
              <strong>{attempt.score} / {attempt.maxScore} marks ({attempt.percent}%)</strong> <Muted>· pass mark {attempt.passPercent}%</Muted>
            </p>
          </div>
          <Badge $tone={attempt.passed ? 'success' : 'warn'}>{attempt.passed ? 'Passed' : 'Not passed yet'}</Badge>
        </Row>
      </Card>
      {attempt.questions.map((q, i) => (
        <Card key={q.id} as="article">
          <QuestionBody q={q} index={i} response={undefined} onChange={() => {}} locked />
          {!q.answered && <p><Badge $tone="warn">Not answered</Badge></p>}
        </Card>
      ))}
    </Stack>
  )
}
