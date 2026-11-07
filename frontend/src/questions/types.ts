import type { ComponentType } from 'react'
import type { QuestionData } from '../types/assessment'

export type QuestionEditorProps = { data: QuestionData; onChange: (data: QuestionData) => void }

export type PlayerProps = {
  /** Question payload as the learner receives it (no answer key). */
  data: QuestionData
  response: QuestionData | undefined
  onChange: (response: QuestionData) => void
  disabled: boolean
  /** Full payload including the answer key; only present once the answer is revealed. */
  answerKey?: QuestionData
}

/**
 * A question type's UI: how authors configure it and how learners answer it.
 * The API holds the matching validation and grading (QuestionTypes.cs).
 */
export type QuestionTypeDef = {
  type: string
  label: string
  defaults: QuestionData
  Editor: ComponentType<QuestionEditorProps>
  Player: ComponentType<PlayerProps>
}
