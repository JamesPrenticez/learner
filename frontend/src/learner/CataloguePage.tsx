import { useState } from 'react'
import { ErrorText, Field, Grid, Input, Row, Select } from '../components/ui'
import { useCurricula } from '../services/builder'
import { useCatalogue } from '../services/learning'
import type { Difficulty } from '../types/course'
import { CourseCard } from './CourseCard'

export function CataloguePage() {
  const [search, setSearch] = useState('')
  const [curriculumId, setCurriculumId] = useState<number | undefined>()
  const [difficulty, setDifficulty] = useState<Difficulty | undefined>()
  const { data: courses, isPending, error } = useCatalogue({ search, curriculumId, difficulty })
  const { data: curricula = [] } = useCurricula()

  return (
    <>
      <h1>Course catalogue</h1>
      <form role="search" onSubmit={(e) => e.preventDefault()}>
        <Row $align="flex-end" style={{ marginBottom: 16 }}>
          <Field label="Search"><Input type="search" value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Title or description" /></Field>
          <Field label="Curriculum">
            <Select value={curriculumId ?? ''} onChange={(e) => setCurriculumId(e.target.value ? Number(e.target.value) : undefined)}>
              <option value="">All</option>
              {curricula.map((c) => <option key={c.id} value={c.id}>{c.title}</option>)}
            </Select>
          </Field>
          <Field label="Difficulty">
            <Select value={difficulty ?? ''} onChange={(e) => setDifficulty((e.target.value || undefined) as Difficulty | undefined)}>
              <option value="">All</option>
              <option>Beginner</option>
              <option>Intermediate</option>
              <option>Advanced</option>
            </Select>
          </Field>
        </Row>
      </form>
      {isPending && <p>Loading…</p>}
      {error && <ErrorText>{error.message}</ErrorText>}
      {courses?.length === 0 && <p>No courses match.</p>}
      <Grid>{courses?.map((c) => <CourseCard key={c.id} course={c} />)}</Grid>
    </>
  )
}
