import type { ComponentType } from 'react'
import type { ExerciseConfig } from '../types/exercise'

export type ExerciseEditorProps = { config: ExerciseConfig; onChange: (config: ExerciseConfig) => void }

export type ExerciseResultView = { summary: string; details: Record<string, unknown>; score: number; maxScore: number }

export type ExercisePlayerProps = {
  /** Configuration as the learner receives it (marking key removed by the server). */
  config: ExerciseConfig
  /** Present when reviewing a past attempt. */
  submission?: Record<string, unknown>
  /** Present after submitting, or when reviewing. The Player renders its own feedback from `details`. */
  result?: ExerciseResultView
  disabled: boolean
  submitting: boolean
  onSubmit: (submission: Record<string, unknown>) => void
}

/**
 * The frontend half of the exercise contract (see .docs/exercise-contract.md).
 * The framework supplies everything around it: loading, enrolment, attempts, history and competency evidence.
 */
export type ExerciseTypeDef = {
  type: string
  label: string
  /** Starting point for a new configuration. May be incomplete: drafts are allowed. */
  defaults: ExerciseConfig
  Editor: ComponentType<ExerciseEditorProps>
  Player: ComponentType<ExercisePlayerProps>
}
