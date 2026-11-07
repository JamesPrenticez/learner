import type { Difficulty } from './course'

export type Competency = { id: number; name: string; description: string }
export type Bank = { id: number; name: string; questions: number }

export type QuestionData = Record<string, unknown>

export type Question = {
  id: number
  bankId: number
  type: string
  text: string
  data: QuestionData
  explanation: string
  hint: string
  difficulty: Difficulty
  marks: number
  category: string
  tags: string[]
  imageMediaId: string | null
  competencyIds: number[]
}

export type QuestionInput = Omit<Question, 'id' | 'bankId'>

export type QuizKind = 'KnowledgeCheck' | 'Quiz' | 'ModuleAssessment' | 'Exam'

export const quizKindLabel: Record<QuizKind, string> = {
  KnowledgeCheck: 'Knowledge check',
  Quiz: 'Quiz',
  ModuleAssessment: 'Module assessment',
  Exam: 'Final exam',
}

export type Quiz = {
  id: number
  title: string
  description: string
  kind: QuizKind
  questionCount: number | null
  passPercent: number
  shuffleQuestions: boolean
  shuffleOptions: boolean
  immediateFeedback: boolean
  timeLimitMinutes: number | null
  maxAttempts: number | null
  availableFrom: string | null
  availableUntil: string | null
  questionIds: number[]
}

export type QuizInput = Omit<Quiz, 'id'>

// ---- learner side

export type QuizInfo = {
  id: number
  courseId: number
  title: string
  description: string
  kind: QuizKind
  questions: number
  passPercent: number
  timeLimitMinutes: number | null
  maxAttempts: number | null
  attemptsUsed: number
  closedReason: string | null
  inProgressAttemptId: number | null
  bestPercent: number | null
}

export type QuestionResult = { score: number; maxScore: number; correct: boolean; explanation: string; answerKey: QuestionData; response: QuestionData | null }

export type AttemptQuestion = {
  id: number
  type: string
  text: string
  hint: string
  marks: number
  imageMediaId: string | null
  data: QuestionData
  answered: boolean
  result: QuestionResult | null
}

export type AttemptView = {
  id: number
  quizId: number
  courseId: number
  title: string
  kind: QuizKind
  passPercent: number
  startedAt: string
  deadline: string | null
  finished: boolean
  score: number
  maxScore: number
  percent: number
  passed: boolean | null
  questions: AttemptQuestion[]
}

export type AnswerResult = { recorded: boolean; result: QuestionResult | null }

export type AttemptSummary = {
  id: number
  quizId: number
  quizTitle: string
  kind: QuizKind
  courseId: number
  courseTitle: string
  startedAt: string
  finishedAt: string | null
  score: number
  maxScore: number
  percent: number
  passed: boolean | null
}
