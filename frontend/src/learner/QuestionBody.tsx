import { Badge, Muted, Stack } from '../components/ui'
import { mediaUrl } from '../services/api'
import { questionType } from '../questions/registry'
import type { AttemptQuestion, QuestionData } from '../types/assessment'
import styled from 'styled-components'
import { theme } from '../styles/theme'

const Feedback = styled.div<{ $ok: boolean }>`
  border-left: 4px solid ${(p) => (p.$ok ? theme.color.success : theme.color.danger)};
  background: ${(p) => (p.$ok ? theme.color.successSoft : theme.color.dangerSoft)};
  border-radius: ${theme.radius};
  padding: 10px 14px;
  p:last-child { margin-bottom: 0; }
`

/** One question as the learner sees it: text, image, the answer control, and (once revealed) feedback. */
export function QuestionBody({ q, index, response, onChange, locked }: {
  q: AttemptQuestion
  index: number
  response: QuestionData | undefined
  onChange: (r: QuestionData) => void
  locked: boolean
}) {
  const def = questionType(q.type)
  return (
    <Stack $gap={12}>
      <div>
        <h3 style={{ margin: 0 }}>Question {index + 1} <Muted>({q.marks} {q.marks === 1 ? 'mark' : 'marks'})</Muted></h3>
        <p style={{ marginTop: 8, whiteSpace: 'pre-wrap' }}>{q.text}</p>
      </div>
      {q.imageMediaId && <img src={mediaUrl(q.imageMediaId)} alt="Supporting figure for the question" style={{ maxHeight: 280, alignSelf: 'flex-start' }} />}
      {q.hint && !locked && (
        <details>
          <summary>Need a hint?</summary>
          <p>{q.hint}</p>
        </details>
      )}
      {def ? (
        <def.Player data={q.data} response={response ?? q.result?.response ?? undefined} onChange={onChange} disabled={locked} answerKey={q.result?.answerKey} />
      ) : (
        <p>This question type is not supported.</p>
      )}
      {q.result && (
        <Feedback $ok={q.result.correct} role="status">
          <p>
            <Badge $tone={q.result.correct ? 'success' : 'danger'}>{q.result.correct ? 'Correct' : q.result.score > 0 ? 'Partly correct' : 'Incorrect'}</Badge>{' '}
            {q.result.score} / {q.result.maxScore}
          </p>
          {q.result.explanation && <p>{q.result.explanation}</p>}
        </Feedback>
      )}
    </Stack>
  )
}
