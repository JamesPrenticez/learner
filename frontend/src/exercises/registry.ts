import { ContractReviewEditor, ContractReviewPlayer } from './ContractReview'
import type { ExerciseTypeDef } from './types'

/**
 * The exercise-component registry. To add a component: write its Editor + Player, add one entry here, and add its
 * ExerciseType to the API's ExerciseTypes.cs. See .docs/exercise-contract.md.
 */
export const exerciseTypes: ExerciseTypeDef[] = [
  {
    type: 'contract-review',
    label: 'Contract review',
    defaults: { scenario: '', instructions: 'Read the clauses and flag any that are problematic.', clauses: [{ id: 'c1', title: '', text: '' }], issues: [] },
    Editor: ContractReviewEditor,
    Player: ContractReviewPlayer,
  },
]

export const exerciseType = (type: string) => exerciseTypes.find((t) => t.type === type)
