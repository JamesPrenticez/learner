import { useState } from 'react'
import { Badge, Button, Card, ErrorText, Field, Input, Row, Select, Stack, Table } from '../components/ui'
import { useAccessActions, useCourseAccess } from '../services/builder'
import type { AccessKind } from '../types/course'
import { useCourse } from './useCourse'

/** Collaborating teachers and (for private courses) invited learners. Only the owner or an admin may change this. */
export function SharingTab() {
  const course = useCourse()
  const { data: rows, error } = useCourseAccess(course.id)
  const { add, remove } = useAccessActions(course.id)
  const [email, setEmail] = useState('')
  const [kind, setKind] = useState<AccessKind>('Invited')

  return (
    <Card>
      <h2>Collaborators and invited learners</h2>
      <p>Owner: {course.ownerName}. {course.visibility === 'Private' ? 'This course is private: only invited learners can enrol.' : 'This course is public: anyone can enrol; invites only matter if you make it private.'}</p>
      <form
        onSubmit={(e) => {
          e.preventDefault()
          add.mutate({ email, kind }, { onSuccess: () => setEmail('') })
        }}
      >
        <Row $align="flex-end">
          <Field label="User email"><Input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} /></Field>
          <Field label="Access">
            <Select value={kind} onChange={(e) => setKind(e.target.value as AccessKind)}>
              <option value="Invited">Invited learner</option>
              <option value="Collaborator">Collaborating teacher</option>
            </Select>
          </Field>
          <Button type="submit" disabled={add.isPending}>Add</Button>
        </Row>
      </form>
      <Stack style={{ marginTop: 12 }}>
        {(error || add.error || remove.error) && <ErrorText>{(error ?? add.error ?? remove.error)!.message}</ErrorText>}
        <Table>
          <tbody>
            {rows?.map((r) => (
              <tr key={r.userId}>
                <td>{r.displayName} ({r.email})</td>
                <td><Badge $tone={r.kind === 'Collaborator' ? 'info' : 'success'}>{r.kind}</Badge></td>
                <td><Button $variant="danger" onClick={() => remove.mutate(r.userId)} aria-label={`Remove ${r.email}`}>Remove</Button></td>
              </tr>
            ))}
          </tbody>
        </Table>
      </Stack>
    </Card>
  )
}
