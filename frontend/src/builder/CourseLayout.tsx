import { Navigate, Outlet, useParams } from 'react-router'
import styled from 'styled-components'
import { TopLink } from '../components/shell'
import { Badge, Button, ButtonLink, ErrorText, Row, Stack } from '../components/ui'
import { useBuilderCourse, useCourseActions } from '../services/builder'
import { theme } from '../styles/theme'
import { statusTone } from '../utils'
import type { CourseCtx } from './useCourse'

const Tabs = styled(Row).attrs({ as: 'nav', 'aria-label': 'Course sections', $gap: 20 })`
  border-bottom: 1px solid ${theme.color.border};
  padding-bottom: 4px;
  a { color: ${theme.color.text}; }
`

/** Course editor frame: title, publish controls, section tabs. Tabs render through the Outlet. */
export function CourseLayout() {
  const id = Number(useParams().courseId)
  const { data: course, isPending, error } = useBuilderCourse(id)
  const actions = useCourseActions(id)

  if (isPending) return <p>Loading…</p>
  if (error) return <ErrorText>{error.message}</ErrorText>

  const failed = [actions.publish, actions.unpublish, actions.archive].find((m) => m.error)?.error

  return (
    <Stack $gap={16}>
      <Row $justify="space-between">
        <Stack $gap={4}>
          <h1 style={{ margin: 0 }}>{course.title}</h1>
          <Row>
            <Badge $tone={statusTone(course.status)}>{course.status}</Badge>
            <Badge>{course.visibility}</Badge>
            <span>v{course.version}</span>
          </Row>
        </Stack>
        <Row>
          <ButtonLink to={`/courses/${course.id}`} $variant="ghost">Preview as learner</ButtonLink>
          {course.status !== 'Published' && <Button onClick={() => actions.publish.mutate()} disabled={actions.publish.isPending}>Publish</Button>}
          {course.status === 'Published' && <Button $variant="ghost" onClick={() => actions.unpublish.mutate()}>Unpublish</Button>}
          {course.status !== 'Archived' && <Button $variant="danger" onClick={() => confirm('Archive this course? It leaves the catalogue; learner records are kept.') && actions.archive.mutate()}>Archive</Button>}
        </Row>
      </Row>
      {failed && <ErrorText>{failed.message}</ErrorText>}
      <Tabs>
        <TopLink to="structure">Structure</TopLink>
        <TopLink to="questions">Questions</TopLink>
        <TopLink to="quizzes">Quizzes</TopLink>
        <TopLink to="exercises">Exercises</TopLink>
        <TopLink to="competencies">Competencies</TopLink>
        <TopLink to="details">Details</TopLink>
        <TopLink to="sharing">Sharing</TopLink>
      </Tabs>
      <Outlet context={{ course } satisfies CourseCtx} />
    </Stack>
  )
}

export const CourseIndexRedirect = () => <Navigate to="structure" replace />
