import { Link } from 'react-router'
import { Cover } from '../components/Cover'
import { Badge, Card, Muted, Stack } from '../components/ui'
import type { CatalogueItem } from '../types/learning'

export function CourseCard({ course }: { course: CatalogueItem }) {
  return (
    <Card as="article">
      <Stack $gap={8}>
        <Cover id={course.coverMediaId} />
        <h3 style={{ margin: 0 }}><Link to={`/courses/${course.id}`}>{course.title}</Link></h3>
        <Muted>by {course.author}{course.curriculum ? ` · ${course.curriculum}` : ''}</Muted>
        <p style={{ margin: 0 }}>{course.description.length > 140 ? `${course.description.slice(0, 140)}…` : course.description}</p>
        <div>
          <Badge>{course.difficulty}</Badge>{' '}
          <Muted>{course.estimatedMinutes} min · {course.lessons} lessons</Muted>{' '}
          {course.enrolled && <Badge $tone="success">Enrolled</Badge>}
        </div>
      </Stack>
    </Card>
  )
}
