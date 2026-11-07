import { useState } from 'react'
import { Link, useParams } from 'react-router'
import styled from 'styled-components'
import { BlockList } from '../blocks/BlockList'
import { blockDef, blockDefs } from '../blocks/registry'
import type { BlockData } from '../blocks/types'
import { Badge, Button, Card, ErrorText, Muted, Row, Select, Stack } from '../components/ui'
import { useCompetencies } from '../services/assessment'
import { useBlockActions, useLessonEdit } from '../services/lessons'
import { theme } from '../styles/theme'
import { moveTo } from '../utils'
import { useCourse } from './useCourse'

const Split = styled.div`
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 16px;
  align-items: start;
  @media (max-width: 900px) { grid-template-columns: minmax(0, 1fr); }
`

const Handle = styled.button`
  cursor: grab;
  border: 1px dashed ${theme.color.border};
  background: transparent;
  border-radius: ${theme.radius};
  padding: 4px 8px;
  font: inherit;
`

/** Remounts per lesson so unsaved drafts never leak between lessons. */
export function LessonEditor() {
  const lessonId = Number(useParams().lessonId)
  return <LessonEditorBody key={lessonId} lessonId={lessonId} />
}

function LessonEditorBody({ lessonId }: { lessonId: number }) {
  const course = useCourse()
  const { data: lesson, isPending, error } = useLessonEdit(lessonId)
  const actions = useBlockActions(lessonId)
  const { data: competencies = [] } = useCompetencies(course.id)
  const [drafts, setDrafts] = useState<Record<number, BlockData>>({})
  const [dragId, setDragId] = useState<number | null>(null)
  const [newType, setNewType] = useState(blockDefs[0].type)

  if (isPending) return <p>Loading…</p>
  if (error) return <ErrorText>{error.message}</ErrorText>

  const ids = lesson.blocks.map((b) => b.id)
  const live = lesson.blocks.map((b) => ({ ...b, data: drafts[b.id] ?? b.data })) // what the preview shows
  const dirty = Object.keys(drafts).length > 0
  const otherLessons = course.modules.flatMap((m) => m.lessons.filter((l) => l.id !== lessonId).map((l) => ({ id: l.id, label: `${m.title} › ${l.title}` })))
  const failure = [actions.add, actions.remove, actions.reorder, actions.copy, actions.saveAll, actions.competencies].find((m) => m.error)?.error

  return (
    <Stack $gap={16}>
      <Row $justify="space-between">
        <h2 style={{ margin: 0 }}>Lesson: {lesson.title}</h2>
        <Link to="../structure">← Back to structure</Link>
      </Row>
      {failure && <ErrorText>{failure.message}</ErrorText>}
      <Split>
        <Stack>
          <Row $justify="space-between">
            <Button disabled={!dirty || actions.saveAll.isPending} onClick={() => actions.saveAll.mutate(drafts, { onSuccess: () => setDrafts({}) })}>Save changes</Button>
            {dirty ? <Badge $tone="warn">Unsaved changes</Badge> : <Muted>All changes saved</Muted>}
          </Row>
          {lesson.blocks.length === 0 && <p>This lesson has no content yet. Add a block below.</p>}
          {lesson.blocks.map((b, i) => {
            const def = blockDef(b.type)
            return (
              <Card
                key={b.id}
                aria-label={`${def?.label ?? b.type} block ${i + 1}`}
                onDragOver={(e) => dragId !== null && e.preventDefault()}
                onDrop={() => {
                  if (dragId === null || dragId === b.id) return
                  actions.reorder.mutate(moveTo(ids, ids.indexOf(dragId), i))
                  setDragId(null)
                }}
              >
                <Row $justify="space-between">
                  <Row>
                    <Handle type="button" draggable aria-label="Drag to reorder" title="Drag to reorder" onDragStart={() => setDragId(b.id)} onDragEnd={() => setDragId(null)}>⠿</Handle>
                    <strong>{def?.label ?? b.type}</strong>
                    {drafts[b.id] && <Badge $tone="warn">edited</Badge>}
                  </Row>
                  <Row>
                    <Button $variant="ghost" aria-label="Move up" disabled={i === 0} onClick={() => actions.reorder.mutate(moveTo(ids, i, i - 1))}>↑</Button>
                    <Button $variant="ghost" aria-label="Move down" disabled={i === ids.length - 1} onClick={() => actions.reorder.mutate(moveTo(ids, i, i + 1))}>↓</Button>
                    <Button $variant="ghost" onClick={() => actions.copy.mutate({ id: b.id })}>Duplicate</Button>
                    <Button $variant="danger" onClick={() => confirm('Delete this block?') && actions.remove.mutate(b.id)}>Delete</Button>
                  </Row>
                </Row>
                <div style={{ marginTop: 12 }}>
                  {def ? <def.Editor courseId={course.id} data={drafts[b.id] ?? b.data} onChange={(data) => setDrafts((d) => ({ ...d, [b.id]: data }))} /> : <p>Unsupported block type.</p>}
                </div>
                {otherLessons.length > 0 && (
                  <Select
                    aria-label="Copy this block to another lesson"
                    style={{ marginTop: 12 }}
                    value=""
                    onChange={(e) => e.target.value && actions.copy.mutate({ id: b.id, lessonId: Number(e.target.value) })}
                  >
                    <option value="">Copy to another lesson…</option>
                    {otherLessons.map((l) => <option key={l.id} value={l.id}>{l.label}</option>)}
                  </Select>
                )}
              </Card>
            )
          })}
          <Card as="fieldset" style={{ margin: 0 }}>
            <legend>Competencies this lesson teaches</legend>
            {competencies.length === 0 && <Muted>Define competencies in the Competencies tab. Lessons are only recommended for them; viewing a lesson is not evidence.</Muted>}
            <Row>
              {competencies.map((c) => (
                <label key={c.id}>
                  <input
                    type="checkbox"
                    checked={lesson.competencyIds.includes(c.id)}
                    disabled={actions.competencies.isPending}
                    onChange={(e) => actions.competencies.mutate(e.target.checked ? [...lesson.competencyIds, c.id] : lesson.competencyIds.filter((x) => x !== c.id))}
                  /> {c.name}
                </label>
              ))}
            </Row>
          </Card>
          <Card>
            <Row>
              <Select aria-label="Block type" value={newType} onChange={(e) => setNewType(e.target.value)} style={{ width: 'auto' }}>
                {blockDefs.map((d) => <option key={d.type} value={d.type}>{d.label}</option>)}
              </Select>
              <Button onClick={() => actions.add.mutate({ type: newType, data: blockDef(newType)!.defaults })} disabled={actions.add.isPending}>Add block</Button>
            </Row>
          </Card>
        </Stack>
        <Card as="aside" aria-label="Learner preview" style={{ position: 'sticky', top: 16 }}>
          <Muted>Learner preview (updates as you type)</Muted>
          <h2>{lesson.title}</h2>
          <BlockList blocks={live} />
        </Card>
      </Split>
    </Stack>
  )
}
