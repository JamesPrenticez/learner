import { Outlet } from 'react-router'
import { Shell, TopLink } from '../components/shell'

export function BuilderLayout() {
  return (
    <Shell
      tone="builder"
      brand="Course Builder"
      to="/builder"
      links={
        <>
          <TopLink to="/builder" end>My courses</TopLink>
          <TopLink to="/">Learner app</TopLink>
        </>
      }
    >
      <Outlet />
    </Shell>
  )
}
