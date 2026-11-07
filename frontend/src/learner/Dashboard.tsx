import { Link } from 'react-router'
import { Cover } from '../components/Cover'
import { Badge, ButtonLink, Card, ErrorText, Grid, Muted, ProgressBar, Row, Stack } from '../components/ui'
import { useMe } from '../services/auth'
import { useMyAttempts } from '../services/assessment'
import { useMyCompetencies } from '../services/competencies'
import { competencyStateLabel, competencyStateTone } from '../types/competency'
import { useCatalogue, useMyCourses } from '../services/learning'
import { CourseCard } from './CourseCard'

export function Dashboard() {
  const { data: me } = useMe()
  const { data: courses, isPending, error } = useMyCourses()
  const { data: invited } = useCatalogue({ scope: 'private' })
  const { data: attempts } = useMyAttempts()
  const { data: competencies } = useMyCompetencies()
  const gaps = competencies?.filter((c) => c.state !== 'Demonstrated') ?? []

  return (
    <Stack $gap={24}>
      <h1>Welcome, {me?.displayName}</h1>

      <section aria-labelledby="my-courses">
        <h2 id="my-courses">My courses</h2>
        {isPending && <p>Loading…</p>}
        {error && <ErrorText>{error.message}</ErrorText>}
        {courses?.length === 0 && <p>You have not enrolled in anything yet. <Link to="/catalogue">Browse the catalogue</Link>.</p>}
        <Grid>
          {courses?.map((c) => {
            const pct = c.total ? Math.round((c.completed / c.total) * 100) : 0
            return (
              <Card as="article" key={c.id}>
                <Stack $gap={8}>
                  <Cover id={c.coverMediaId} />
                  <h3 style={{ margin: 0 }}><Link to={`/courses/${c.id}`}>{c.title}</Link></h3>
                  <ProgressBar $pct={pct} role="progressbar" aria-label={`${c.title} progress`} aria-valuenow={pct} aria-valuemin={0} aria-valuemax={100} />
                  <Row $justify="space-between">
                    <Muted>{c.completed} of {c.total} lessons</Muted>
                    {c.continueLessonId && <ButtonLink to={`/courses/${c.id}/lessons/${c.continueLessonId}`}>Continue</ButtonLink>}
                  </Row>
                </Stack>
              </Card>
            )
          })}
        </Grid>
      </section>

      {competencies && competencies.length > 0 && (
        <section aria-labelledby="competency-overview">
          <h2 id="competency-overview">Competency overview</h2>
          <p>
            <Badge $tone="success">{competencies.filter((c) => c.state === 'Demonstrated').length} demonstrated</Badge>{' '}
            <Badge $tone="warn">{competencies.filter((c) => c.state === 'Developing').length} developing</Badge>{' '}
            <Badge>{competencies.filter((c) => c.state === 'NotAssessed').length} not assessed</Badge>{' '}
            <Link to="/competencies">See all</Link>
          </p>
          {gaps.length > 0 && (
            <Card>
              <h3>Areas to develop</h3>
              <Stack $gap={8}>
                {gaps.slice(0, 5).map((c) => (
                  <div key={c.id}>
                    <Badge $tone={competencyStateTone[c.state]}>{competencyStateLabel[c.state]}</Badge> <strong>{c.name}</strong> <Muted>· {c.courseTitle}</Muted>
                    {c.recommended[0] && <> · Try <Link to={`/courses/${c.recommended[0].courseId}/lessons/${c.recommended[0].id}`}>{c.recommended[0].title}</Link></>}
                  </div>
                ))}
              </Stack>
            </Card>
          )}
        </section>
      )}

      <section aria-labelledby="recent-results">
        <h2 id="recent-results">Recent assessment results</h2>
        {attempts?.length === 0 && <p>No assessments completed yet.</p>}
        <Stack $gap={8}>
          {attempts?.slice(0, 5).map((a) => (
            <Row key={a.id} $justify="space-between">
              <Link to={`/results/${a.id}`}>{a.quizTitle}</Link>
              <Row>
                <Muted>{a.percent}%</Muted>
                <Badge $tone={a.passed ? 'success' : 'warn'}>{a.passed ? 'Passed' : 'Not passed'}</Badge>
              </Row>
            </Row>
          ))}
        </Stack>
        {attempts && attempts.length > 5 && <p><Link to="/results">All results</Link></p>}
      </section>

      {invited && invited.length > 0 && (
        <section aria-labelledby="private-courses">
          <h2 id="private-courses">Private courses you were invited to</h2>
          <Grid>{invited.map((c) => <CourseCard key={c.id} course={c} />)}</Grid>
        </section>
      )}
    </Stack>
  )
}
