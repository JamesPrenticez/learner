export type Role = 'Learner' | 'Teacher' | 'Admin'

export type Me = {
  id: string
  email: string
  displayName: string
  roles: Role[]
}
