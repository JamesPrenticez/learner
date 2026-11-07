import { useState } from 'react'
import { AddForm } from '../components/forms'
import { Button, Card, ErrorText, Field, Input, Muted, Row, Select, Stack, Textarea } from '../components/ui'
import { mediaUrl } from '../services/api'
import { useAddCurriculum, useCourseActions, useCurricula } from '../services/builder'
import { useUpload } from '../services/media'
import type { CourseUpdate, Difficulty, Visibility } from '../types/course'
import { useCourse } from './useCourse'

export function DetailsTab() {
  const course = useCourse()
  const { update } = useCourseActions(course.id)
  const { data: curricula = [] } = useCurricula()
  const addCurriculum = useAddCurriculum()
  const upload = useUpload()
  const [form, setForm] = useState<CourseUpdate & { objectives: string }>({
    ...course,
    objectives: course.learningObjectives.join('\n'),
  })
  const set = <K extends keyof typeof form>(k: K, v: (typeof form)[K]) => setForm((f) => ({ ...f, [k]: v }))

  return (
    <Card>
      <form
        onSubmit={(e) => {
          e.preventDefault()
          const { objectives, ...fields } = form
          update.mutate({
            title: fields.title,
            description: fields.description,
            coverMediaId: fields.coverMediaId,
            curriculumId: fields.curriculumId,
            visibility: fields.visibility,
            difficulty: fields.difficulty,
            estimatedMinutes: fields.estimatedMinutes,
            learningObjectives: objectives.split('\n'),
          })
        }}
      >
        <Stack>
          <Field label="Title"><Input required value={form.title} onChange={(e) => set('title', e.target.value)} /></Field>
          <Field label="Description"><Textarea value={form.description} onChange={(e) => set('description', e.target.value)} /></Field>
          <Field label="Cover image">
            <input
              type="file"
              accept="image/png,image/jpeg,image/gif,image/webp"
              onChange={(e) => {
                const file = e.target.files?.[0]
                if (file) upload.mutate(file, { onSuccess: (m) => set('coverMediaId', m.id) })
              }}
            />
          </Field>
          {upload.error && <ErrorText>{upload.error.message}</ErrorText>}
          {form.coverMediaId && <img src={mediaUrl(form.coverMediaId)} alt="Course cover preview" style={{ maxHeight: 100, alignSelf: 'flex-start' }} />}
          <Field label="Learning objectives (one per line)"><Textarea value={form.objectives} onChange={(e) => set('objectives', e.target.value)} /></Field>
          <Row $align="flex-end">
            <Field label="Curriculum">
              <Select value={form.curriculumId ?? ''} onChange={(e) => set('curriculumId', e.target.value ? Number(e.target.value) : null)}>
                <option value="">None</option>
                {curricula.map((c) => <option key={c.id} value={c.id}>{c.title}</option>)}
              </Select>
            </Field>
            <Muted>or</Muted>
            <AddForm label="New curriculum" button="Create" onAdd={(title) => addCurriculum.mutate(title, { onSuccess: (c) => set('curriculumId', c.id) })} />
          </Row>
          <Row>
            <Field label="Visibility">
              <Select value={form.visibility} onChange={(e) => set('visibility', e.target.value as Visibility)}>
                <option value="Public">Public (in the catalogue)</option>
                <option value="Private">Private (invited learners only)</option>
              </Select>
            </Field>
            <Field label="Difficulty">
              <Select value={form.difficulty} onChange={(e) => set('difficulty', e.target.value as Difficulty)}>
                <option>Beginner</option>
                <option>Intermediate</option>
                <option>Advanced</option>
              </Select>
            </Field>
            <Field label="Estimated duration (minutes)">
              <Input type="number" min={0} value={form.estimatedMinutes} onChange={(e) => set('estimatedMinutes', Number(e.target.value))} />
            </Field>
          </Row>
          {update.error && <ErrorText>{update.error.message}</ErrorText>}
          <Row>
            <Button type="submit" disabled={update.isPending}>Save details</Button>
            {update.isSuccess && <Muted role="status">Saved.</Muted>}
          </Row>
        </Stack>
      </form>
    </Card>
  )
}
