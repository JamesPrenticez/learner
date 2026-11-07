export type CompetencyState = 'NotAssessed' | 'Developing' | 'Demonstrated' | 'RequiresReview'

export const competencyStateLabel: Record<CompetencyState, string> = {
  NotAssessed: 'Not assessed',
  Developing: 'Developing',
  Demonstrated: 'Demonstrated',
  RequiresReview: 'Requires review',
}

export const competencyStateTone: Record<CompetencyState, 'info' | 'warn' | 'success' | 'danger'> = {
  NotAssessed: 'info',
  Developing: 'warn',
  Demonstrated: 'success',
  RequiresReview: 'danger',
}

export type RecommendedLesson = { id: number; title: string; courseId: number; completed: boolean }

export type CompetencyStatus = {
  id: number
  name: string
  courseId: number
  courseTitle: string
  state: CompetencyState
  percent: number
  evidence: number
  lastEvidence: string | null
  recommended: RecommendedLesson[]
}

export type EvidenceRow = { id: number; source: string; sourceId: number; title: string; percent: number; weight: number; at: string }
