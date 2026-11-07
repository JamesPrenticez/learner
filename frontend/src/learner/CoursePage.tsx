import { Link, useParams } from 'react-router'
import { Cover } from '../components/Cover'
import { Badge, Button, ButtonLink, Card, ErrorText, Muted, ProgressBar, Row, Stack } from '../components/ui'
import { useCourseOverview, useEnrol, useUnenrol } from '../services/learning'

export function CoursePage() {
  const id = Number(useParams().courseId)
  const { data: course, isPending, error } = useCourseOverview(id)
  const enrol = useEnrol(id)
  const unenrol = useUnenrol(id)

  if (isPending) return <p>Loading…</p>
  if (error) return <ErrorText>This course is not available.</ErrorText>

  const pct = course.total ? Math.round((course.completed / course.total) * 100) : 0
  const firstLesson = course.modules.flatMap((m) => m.lessons)[0]
  const failure = enrol.error ?? unenrol.error

  return (
    <Stack $gap={16}>
      {course.status !== 'Published' && (
        <Card role="status">
          <Badge $tone="warn">{course.status === 'Draft' ? 'Preview' : 'Archived'}</Badge>{' '}
          {course.status === 'Draft' ? 'This draft is only visible to its authors.' : 'This course is archived and closed to new enrolment.'}
          {course.canManage && <> <Link to={`/builder/courses/${course.id}`}>Edit in the Course Builder</Link></>}
        </Card>
      )}
      <Row $align="flex-start" $gap={24}>
        <div style={{ flex: '1 1 420px' }}>
          <h1>{course.title}</h1>
          <Muted>by {course.author}{course.curriculum ? ` · ${course.curriculum}` : ''}</Muted>
          <p style={{ whiteSpace: 'pre-wrap', marginTop: 12 }}>{course.description}</p>
          <Row>
            <Badge>{course.difficulty}</Badge>
            <Badge>{course.visibility}</Badge>
            <Muted>{course.estimatedMinutes} min · v{course.version}</Muted>
          </Row>
        </div>
        <div style={{ flex: '0 1 320px', width: '100%' }}><Cover id={course.coverMediaId} /></div>
      </Row>

      {course.learningObjectives.length > 0 && (
        <Card>
          <h2>What you will learn</h2>
          <ul>{course.learningObjectives.map((o, i) => <li key={i}>{o}</li>)}</ul>
        </Card>
      )}

      <Card>
        <Row $justify="space-between">
          <div style={{ flex: '1 1 240px' }}>
            <strong>{course.enrolled ? `${course.completed} of ${course.total} lessons complete` : `${course.total} lessons`}</strong>
            {course.enrolled && <ProgressBar $pct={pct} role="progressbar" aria-label="Course progress" aria-valuenow={pct} aria-valuemin={0} aria-valuemax={100} />}
          </div>
          <Row>
            {firstLesson && <ButtonLink to={`lessons/${firstLesson.id}`} $variant="ghost">Start anywhere</ButtonLink>}
            {course.enrolled ? (
              <Button $variant="ghost" onClick={() => confirm('Unenrol? Your history is kept.') && unenrol.mutate(undefined)}>Unenrol</Button>
            ) : (
              course.canEnrol && <Button onClick={() => enrol.mutate(undefined)} disabled={enrol.isPending}>Enrol</Button>
            )}
          </Row>
        </Row>
        {failure && <ErrorText>{failure.message}</ErrorText>}
      </Card>

      <Card>
        <h2>Course content</h2>
        <p><Muted>Learn in any order. The sequence below is a recommendation, not a requirement.</Muted></p>
        {course.modules.map((m) => (
          <section key={m.id} aria-label={m.title}>
            <h3>{m.title}</h3>
            <ul style={{ listStyle: 'none', padding: 0 }}>
              {m.lessons.map((l) => (
                <li key={l.id} style={{ padding: '4px 0' }}>
                  <span aria-hidden="true">{l.completed ? '✅' : '⚪'}</span>{' '}
                  <Link to={`lessons/${l.id}`}>{l.title}</Link>
                  {l.completed && <span className="sr-only"> (completed)</span>}
                </li>
              ))}
            </ul>
          </section>
        ))}
      </Card>
    </Stack>
  )
}
