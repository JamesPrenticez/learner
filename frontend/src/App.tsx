import { useQuery } from '@tanstack/react-query'
import { fetchUser } from './services/user'

function App() {
  const { data: user, isPending, error } = useQuery({ queryKey: ['user'], queryFn: fetchUser })

  if (isPending) return <p>Loading...</p>
  if (error) return <p>Error: {error.message}</p>

  return (
    <>
      <h1>Hello, {user.name}</h1>
      <p>{user.email}</p>
    </>
  )
}

export default App
