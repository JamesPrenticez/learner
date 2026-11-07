import { Card, ErrorText, Table } from '../../components/ui'
import { useSetRoles, useUsers } from '../../services/auth'
import type { Role } from '../../types/auth'

const ROLES: Role[] = ['Learner', 'Teacher', 'Admin']

export function UsersPage() {
  const { data: users, isPending, error } = useUsers()
  const setRoles = useSetRoles()

  if (isPending) return <p>Loading…</p>
  if (error) return <ErrorText>{error.message}</ErrorText>

  return (
    <Card>
      <h1>Users and roles</h1>
      {setRoles.error && <ErrorText>{setRoles.error.message}</ErrorText>}
      <Table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Roles</th>
          </tr>
        </thead>
        <tbody>
          {users.map((u) => (
            <tr key={u.id}>
              <td>{u.displayName}</td>
              <td>{u.email}</td>
              <td>
                {ROLES.map((r) => (
                  <label key={r} style={{ marginRight: 12 }}>
                    <input
                      type="checkbox"
                      checked={u.roles.includes(r)}
                      disabled={setRoles.isPending}
                      onChange={(e) => setRoles.mutate({ id: u.id, roles: e.target.checked ? [...u.roles, r] : u.roles.filter((x) => x !== r) })}
                    />{' '}
                    {r}
                  </label>
                ))}
              </td>
            </tr>
          ))}
        </tbody>
      </Table>
    </Card>
  )
}
