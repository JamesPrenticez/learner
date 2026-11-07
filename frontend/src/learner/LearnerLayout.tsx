import { Outlet } from 'react-router'
import { Shell, TopLink } from '../components/shell'
import { hasRole, useMe } from '../services/auth'

export function LearnerLayout() {
  const { data: me } = useMe()
  return (
    <Shell
      tone="learner"
      brand="Learner"
      to="/"
      links={
        <>
          <TopLink to="/" end>Dashboard</TopLink>
          <TopLink to="/catalogue">Catalogue</TopLink>
          <TopLink to="/competencies">Competencies</TopLink>
          <TopLink to="/results">Results</TopLink>
          {hasRole(me, 'Teacher', 'Admin') && <TopLink to="/builder">Course Builder</TopLink>}
          {hasRole(me, 'Admin') && <TopLink to="/admin/users">Users</TopLink>}
        </>
      }
    >
      <Outlet />
    </Shell>
  )
}
