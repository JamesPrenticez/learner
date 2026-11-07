export type Visibility = 'Public' | 'Private'
export type CourseStatus = 'Draft' | 'Published' | 'Archived'
export type Difficulty = 'Beginner' | 'Intermediate' | 'Advanced'

export type Curriculum = { id: number; title: string; description: string }

export type CourseSummary = {
  id: number
  title: string
  status: CourseStatus
  visibility: Visibility
  version: number
  updatedAt: string
  modules: number
  lessons: number
}

export type Lesson = { id: number; title: string; order: number }
export type Module = { id: number; title: string; order: number; lessons: Lesson[] }

export type CourseDetail = {
  id: number
  title: string
  description: string
  coverMediaId: string | null
  curriculumId: number | null
  ownerId: string
  ownerName: string
  visibility: Visibility
  status: CourseStatus
  difficulty: Difficulty
  estimatedMinutes: number
  learningObjectives: string[]
  version: number
  createdAt: string
  updatedAt: string
  modules: Module[]
}

export type CourseUpdate = Pick<
  CourseDetail,
  'title' | 'description' | 'coverMediaId' | 'curriculumId' | 'visibility' | 'difficulty' | 'estimatedMinutes' | 'learningObjectives'
>

export type AccessKind = 'Collaborator' | 'Invited'
export type AccessRow = { userId: string; email: string; displayName: string; kind: AccessKind }
