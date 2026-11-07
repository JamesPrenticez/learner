import { MultipleEditor, MultiplePlayer, SingleEditor, SinglePlayer } from './Choice'
import { FillEditor, FillPlayer } from './Fill'
import { NumericEditor, NumericPlayer } from './Numeric'
import { TrueFalseEditor, TrueFalsePlayer } from './TrueFalse'
import type { QuestionTypeDef } from './types'

const twoOptions = [{ id: 'a', text: '' }, { id: 'b', text: '' }]

/**
 * The question-type registry. To add a type (matching, ordering, …): write its Editor + Player, add one entry here,
 * and add its validation and grading entry in the API's QuestionTypes.cs.
 */
export const questionTypes: QuestionTypeDef[] = [
  { type: 'single', label: 'Single choice', defaults: { options: twoOptions, correct: 'a' }, Editor: SingleEditor, Player: SinglePlayer },
  { type: 'multiple', label: 'Multiple choice', defaults: { options: twoOptions, correct: ['a'] }, Editor: MultipleEditor, Player: MultiplePlayer },
  { type: 'truefalse', label: 'True / False', defaults: { correct: true }, Editor: TrueFalseEditor, Player: TrueFalsePlayer },
  { type: 'fillblank', label: 'Fill in the blank', defaults: { accepted: [], caseSensitive: false }, Editor: FillEditor, Player: FillPlayer },
  { type: 'numeric', label: 'Numerical', defaults: { tolerance: 0 }, Editor: NumericEditor, Player: NumericPlayer },
]

export const questionType = (type: string) => questionTypes.find((t) => t.type === type)
