import { Link, useParams } from 'react-router'
import styled from 'styled-components'
import { BlockList } from '../blocks/BlockList'
import { Button, ButtonLink, Card, ErrorText, Muted, Row, Stack } from '../components/ui'
import { useComplete, useCourseOverview, useEnrol, useLesson } from '../services/learning'
import { theme } from '../styles/theme'

const Layout = styled.div`
  display: grid;
  grid-template-columns: 260px minmax(0, 1fr);
  gap: 24px;
  align-items: start;
  @media (max-width: 800px) { grid-template-columns: minmax(0, 1fr); }
`

const Outline = styled.nav`
  ul { list-style: none; padding: 0; margin: 0 0 12px; }
  li a { display: block; padding: 4px 8px; border-radius: ${theme.radius}; text-decoration: none; color: ${theme.color.text}; }
  li a[aria-current='page'] { background: ${theme.color.primarySoft}; font-weight: 600; }
`

export function LessonPage() {
  const courseId = Number(useParams().courseId)
  const lessonId = Number(useParams().lessonId)
  const { data: lesson, isPending, error } = useLesson(lessonId)
  const { data: course } = useCourseOverview(courseId)
  const complete = useComplete(lessonId)
  const enrol = useEnrol(courseId)

  if (isPending) return <p>Loading…</p>
  if (error) return <ErrorText>This lesson is not available.</ErrorText>

  return (
    <Layout>
      <Outline aria-label="Course navigation">
        <Link to={`/courses/${courseId}`}>← {lesson.courseTitle}</Link>
        {course?.modules.map((m) => (
          <section key={m.id} aria-label={m.title} style={{ marginTop: 12 }}>
            <strong>{m.title}</strong>
            <ul>
              {m.lessons.map((l) => (
                <li key={l.id}>
                  <Link to={`/courses/${courseId}/lessons/${l.id}`} aria-current={l.id === lessonId ? 'page' : undefined}>
                    <span aria-hidden="true">{l.completed ? '✅' : '⚪'}</span> {l.title}
                    {l.completed && <span className="sr-only"> (completed)</span>}
                  </Link>
                </li>
              ))}
            </ul>
          </section>
        ))}
      </Outline>

      <Stack $gap={16}>
        <div>
          <Muted>{lesson.moduleTitle}</Muted>
          <h1>{lesson.title}</h1>
        </div>
        {!lesson.enrolled && (
          <Card role="status">
            <Row $justify="space-between">
              <span>Enrol in this course to track your progress and quiz results.</span>
              {course?.canEnrol && <Button onClick={() => enrol.mutate(undefined)} disabled={enrol.isPending}>Enrol</Button>}
            </Row>
          </Card>
        )}
        <BlockList blocks={lesson.blocks} />
        {complete.error && <ErrorText>{complete.error.message}</ErrorText>}
        <Card>
          <Row $justify="space-between">
            {lesson.prevId ? <ButtonLink $variant="ghost" to={`/courses/${courseId}/lessons/${lesson.prevId}`}>← Previous</ButtonLink> : <span />}
            {lesson.enrolled && (
              <Button $variant={lesson.completed ? 'ghost' : 'primary'} onClick={() => complete.mutate(!lesson.completed)} disabled={complete.isPending}>
                {lesson.completed ? '✓ Completed (undo)' : 'Mark as complete'}
              </Button>
            )}
            {lesson.nextId ? <ButtonLink to={`/courses/${courseId}/lessons/${lesson.nextId}`}>Next →</ButtonLink> : <span />}
          </Row>
        </Card>
      </Stack>
    </Layout>
  )
}
