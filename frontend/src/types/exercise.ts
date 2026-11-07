export type ExerciseConfig = Record<string, unknown>

export type ExerciseTypeInfo = { key: string; label: string }
export type ExerciseSummary = { id: number; title: string; typeKey: string; version: number; problem: string | null }

export type ExerciseDetail = {
  id: number
  courseId: number
  typeKey: string
  title: string
  config: ExerciseConfig
  version: number
  competencyIds: number[]
  problem: string | null
}

/** What a learner receives before submitting: the config with the marking key already removed by the server. */
export type ExerciseView = {
  id: number
  courseId: number
  title: string
  typeKey: string
  version: number
  config: ExerciseConfig
  attempts: number
  bestPercent: number | null
  problem: string | null
}

export type ExerciseAttempt = {
  id: number
  exerciseId: number
  title: string
  typeKey: string
  configVersion: number
  config: ExerciseConfig
  submission: Record<string, unknown>
  summary: string
  details: Record<string, unknown>
  score: number
  maxScore: number
  percent: number
  submittedAt: string
}

export type ExerciseAttemptSummary = {
  id: number
  exerciseId: number
  title: string
  courseId: number
  courseTitle: string
  score: number
  maxScore: number
  percent: number
  submittedAt: string
}
