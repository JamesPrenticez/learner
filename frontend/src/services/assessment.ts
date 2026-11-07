import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type {
  AnswerResult, AttemptSummary, AttemptView, Bank, Competency, Question, QuestionData, QuestionInput, Quiz, QuizInfo, QuizInput, QuizKind,
} from '../types/assessment'
import { api } from './api'

// ---- builder: competencies, banks, questions, quizzes

export const useCompetencies = (courseId: number) =>
  useQuery({ queryKey: ['builder', 'competencies', courseId], queryFn: () => api<Competency[]>(`/api/builder/courses/${courseId}/competencies`) })

export const useBanks = (courseId: number) =>
  useQuery({ queryKey: ['builder', 'banks', courseId], queryFn: () => api<Bank[]>(`/api/builder/courses/${courseId}/banks`) })

export const useQuestions = (courseId: number) =>
  useQuery({ queryKey: ['builder', 'questions', courseId], queryFn: () => api<Question[]>(`/api/builder/courses/${courseId}/questions`) })

export const useQuizzes = (courseId: number) =>
  useQuery({ queryKey: ['builder', 'quizzes', courseId], queryFn: () => api<Quiz[]>(`/api/builder/courses/${courseId}/quizzes`) })

/** Builder mutations refresh whichever lists they touch. */
function useBuilderMutation<V, R = unknown>(courseId: number, keys: string[], fn: (v: V) => Promise<R>) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () => Promise.all(keys.map((k) => qc.invalidateQueries({ queryKey: ['builder', k, courseId] }))),
  })
}

export function useCompetencyActions(courseId: number) {
  const keys = ['competencies', 'questions']
  return {
    add: useBuilderMutation(courseId, keys, (v: { name: string; description?: string }) => api(`/api/builder/courses/${courseId}/competencies`, { body: v })),
    update: useBuilderMutation(courseId, keys, (v: { id: number; name: string; description: string }) => api(`/api/builder/competencies/${v.id}`, { method: 'PUT', body: v })),
    remove: useBuilderMutation(courseId, keys, (id: number) => api(`/api/builder/competencies/${id}`, { method: 'DELETE' })),
  }
}

export function useBankActions(courseId: number) {
  const keys = ['banks', 'questions', 'quizzes']
  return {
    add: useBuilderMutation(courseId, keys, (name: string) => api<Bank>(`/api/builder/courses/${courseId}/banks`, { body: { name } })),
    rename: useBuilderMutation(courseId, keys, (v: { id: number; name: string }) => api(`/api/builder/banks/${v.id}`, { method: 'PUT', body: { name: v.name } })),
    remove: useBuilderMutation(courseId, keys, (id: number) => api(`/api/builder/banks/${id}`, { method: 'DELETE' })),
  }
}

export function useQuestionActions(courseId: number) {
  const keys = ['questions', 'banks', 'quizzes']
  return {
    create: useBuilderMutation(courseId, keys, (v: { bankId: number; input: QuestionInput }) => api<Question>(`/api/builder/banks/${v.bankId}/questions`, { body: v.input })),
    update: useBuilderMutation(courseId, keys, (v: { id: number; input: QuestionInput }) => api<Question>(`/api/builder/questions/${v.id}`, { method: 'PUT', body: v.input })),
    remove: useBuilderMutation(courseId, keys, (id: number) => api(`/api/builder/questions/${id}`, { method: 'DELETE' })),
  }
}

export function useQuizActions(courseId: number) {
  const keys = ['quizzes']
  return {
    create: useBuilderMutation(courseId, keys, (v: { title: string; kind: QuizKind }) => api<Quiz>(`/api/builder/courses/${courseId}/quizzes`, { body: v })),
    update: useBuilderMutation(courseId, keys, (v: { id: number; input: QuizInput }) => api<Quiz>(`/api/builder/quizzes/${v.id}`, { method: 'PUT', body: v.input })),
    remove: useBuilderMutation(courseId, keys, (id: number) => api(`/api/builder/quizzes/${id}`, { method: 'DELETE' })),
  }
}

// ---- learner: taking quizzes

export const useQuizInfo = (id: number) => useQuery({ queryKey: ['quiz', id], queryFn: () => api<QuizInfo>(`/api/quizzes/${id}`) })

export const useAttempt = (id: number | undefined) =>
  useQuery({ queryKey: ['attempt', id], enabled: id !== undefined, queryFn: () => api<AttemptView>(`/api/attempts/${id}`) })

export const useMyAttempts = (courseId?: number) =>
  useQuery({ queryKey: ['attempts', courseId], queryFn: () => api<AttemptSummary[]>(`/api/me/attempts${courseId ? `?courseId=${courseId}` : ''}`) })

/** Starting, answering and finishing change what the dashboard and quiz cards show. */
export function useAttemptActions(quizId: number) {
  const qc = useQueryClient()
  const refresh = () => Promise.all(['quiz', 'attempts', 'attempt', 'competencies'].map((k) => qc.invalidateQueries({ queryKey: [k] })))
  return {
    start: useMutation({ mutationFn: () => api<AttemptView>(`/api/quizzes/${quizId}/attempts`, { body: {} }), onSuccess: refresh }),
    answer: useMutation({
      mutationFn: (v: { attemptId: number; questionId: number; response: QuestionData }) =>
        api<AnswerResult>(`/api/attempts/${v.attemptId}/answers`, { body: { questionId: v.questionId, response: v.response } }),
    }),
    finish: useMutation({ mutationFn: (attemptId: number) => api<AttemptView>(`/api/attempts/${attemptId}/finish`, { body: {} }), onSuccess: refresh }),
  }
}
