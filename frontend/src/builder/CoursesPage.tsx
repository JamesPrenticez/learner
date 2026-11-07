import { useNavigate } from 'react-router'
import { AddForm } from '../components/forms'
import { Badge, ButtonLink, Card, ErrorText, Row, Stack } from '../components/ui'
import { useBuilderCourses, useCreateCourse } from '../services/builder'
import { statusTone } from '../utils'

export function CoursesPage() {
  const { data: courses, isPending, error } = useBuilderCourses()
  const create = useCreateCourse()
  const navigate = useNavigate()

  return (
    <Stack $gap={16}>
      <h1>My courses</h1>
      <Card>
        <h2>Create a course</h2>
        <AddForm label="Course title" button="Create draft" disabled={create.isPending} onAdd={(title) => create.mutate(title, { onSuccess: ({ id }) => navigate(`/builder/courses/${id}`) })} />
        {create.error && <ErrorText>{create.error.message}</ErrorText>}
      </Card>
      {isPending && <p>Loading…</p>}
      {error && <ErrorText>{error.message}</ErrorText>}
      {courses?.length === 0 && <p>No courses yet.</p>}
      {courses?.map((c) => (
        <Card key={c.id}>
          <Row $justify="space-between">
            <Stack $gap={4}>
              <h3 style={{ margin: 0 }}>{c.title}</h3>
              <Row>
                <Badge $tone={statusTone(c.status)}>{c.status}</Badge>
                <Badge>{c.visibility}</Badge>
                <span>{c.modules} modules · {c.lessons} lessons · v{c.version}</span>
              </Row>
            </Stack>
            <ButtonLink to={`/builder/courses/${c.id}`}>Edit</ButtonLink>
          </Row>
        </Card>
      ))}
    </Stack>
  )
}
