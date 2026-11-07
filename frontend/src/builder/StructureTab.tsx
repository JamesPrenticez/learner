import { Link } from 'react-router'
import { AddForm, InlineEdit } from '../components/forms'
import { Button, Card, ErrorText, Row, Stack } from '../components/ui'
import { useCourseActions } from '../services/builder'
import { moveTo } from '../utils'
import { useCourse } from './useCourse'

export function StructureTab() {
  const course = useCourse()
  const a = useCourseActions(course.id)
  const error = [a.addModule, a.renameModule, a.deleteModule, a.reorderModules, a.addLesson, a.renameLesson, a.deleteLesson, a.reorderLessons].find((m) => m.error)?.error
  const moduleIds = course.modules.map((m) => m.id)

  return (
    <Stack $gap={16}>
      {error && <ErrorText>{error.message}</ErrorText>}
      {course.modules.length === 0 && <p>Add a module to start building the course.</p>}
      {course.modules.map((m, mi) => {
        const lessonIds = m.lessons.map((l) => l.id)
        return (
          <Card key={m.id} as="section" aria-label={`Module ${m.title}`}>
            <Row $justify="space-between">
              <InlineEdit value={m.title} onSave={(title) => a.renameModule.mutate({ id: m.id, title })} />
              <Row>
                <Button $variant="ghost" aria-label={`Move ${m.title} up`} disabled={mi === 0} onClick={() => a.reorderModules.mutate(moveTo(moduleIds, mi, mi - 1))}>↑</Button>
                <Button $variant="ghost" aria-label={`Move ${m.title} down`} disabled={mi === moduleIds.length - 1} onClick={() => a.reorderModules.mutate(moveTo(moduleIds, mi, mi + 1))}>↓</Button>
                <Button $variant="danger" aria-label={`Delete module ${m.title}`} onClick={() => confirm(`Delete module "${m.title}" and its lessons?`) && a.deleteModule.mutate(m.id)}>Delete</Button>
              </Row>
            </Row>
            <Stack $gap={8} style={{ marginTop: 12 }}>
              {m.lessons.map((l, li) => (
                <Row key={l.id} $justify="space-between">
                  <Row>
                    <Link to={`lessons/${l.id}`}>{l.title}</Link>
                    <InlineEdit value={l.title} onSave={(title) => a.renameLesson.mutate({ id: l.id, title })} />
                  </Row>
                  <Row>
                    <Button $variant="ghost" aria-label={`Move ${l.title} up`} disabled={li === 0} onClick={() => a.reorderLessons.mutate({ moduleId: m.id, ids: moveTo(lessonIds, li, li - 1) })}>↑</Button>
                    <Button $variant="ghost" aria-label={`Move ${l.title} down`} disabled={li === lessonIds.length - 1} onClick={() => a.reorderLessons.mutate({ moduleId: m.id, ids: moveTo(lessonIds, li, li + 1) })}>↓</Button>
                    <Button $variant="danger" aria-label={`Delete lesson ${l.title}`} onClick={() => confirm(`Delete lesson "${l.title}"?`) && a.deleteLesson.mutate(l.id)}>Delete</Button>
                  </Row>
                </Row>
              ))}
              <AddForm label={`New lesson in ${m.title}`} button="Add lesson" onAdd={(title) => a.addLesson.mutate({ moduleId: m.id, title })} />
            </Stack>
          </Card>
        )
      })}
      <Card>
        <AddForm label="New module title" button="Add module" onAdd={(title) => a.addModule.mutate(title)} />
      </Card>
    </Stack>
  )
}
