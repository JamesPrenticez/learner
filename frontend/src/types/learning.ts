import type { Block } from '../blocks/types'
import type { CourseStatus, Difficulty, Visibility } from './course'

export type CatalogueItem = {
  id: number
  title: string
  description: string
  coverMediaId: string | null
  curriculum: string | null
  author: string
  difficulty: Difficulty
  estimatedMinutes: number
  lessons: number
  enrolled: boolean
}

export type OutlineLesson = { id: number; title: string; completed: boolean }
export type OutlineModule = { id: number; title: string; lessons: OutlineLesson[] }

export type CourseOverview = {
  id: number
  title: string
  description: string
  coverMediaId: string | null
  curriculum: string | null
  author: string
  difficulty: Difficulty
  estimatedMinutes: number
  learningObjectives: string[]
  status: CourseStatus
  visibility: Visibility
  version: number
  enrolled: boolean
  canManage: boolean
  canEnrol: boolean
  completed: number
  total: number
  modules: OutlineModule[]
}

export type LessonView = {
  id: number
  title: string
  courseId: number
  courseTitle: string
  moduleTitle: string
  enrolled: boolean
  completed: boolean
  prevId: number | null
  nextId: number | null
  blocks: Block[]
}

export type MyCourse = {
  id: number
  title: string
  coverMediaId: string | null
  completed: number
  total: number
  continueLessonId: number | null
  lastActivity: string
}

export type CatalogueFilters = { search?: string; curriculumId?: number; difficulty?: Difficulty; scope?: 'private' }
