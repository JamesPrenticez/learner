import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { ExerciseAttempt, ExerciseAttemptSummary, ExerciseConfig, ExerciseDetail, ExerciseSummary, ExerciseTypeInfo, ExerciseView } from '../types/exercise'
import { api } from './api'

// ---- builder

export const useExerciseTypes = () => useQuery({ queryKey: ['builder', 'exercise-types'], queryFn: () => api<ExerciseTypeInfo[]>('/api/builder/exercise-types') })

export const useExercises = (courseId: number) =>
  useQuery({ queryKey: ['builder', 'exercises', courseId], queryFn: () => api<ExerciseSummary[]>(`/api/builder/courses/${courseId}/exercises`) })

export const useExerciseDetail = (id: number | null) =>
  useQuery({ queryKey: ['builder', 'exercise', id], enabled: id !== null, queryFn: () => api<ExerciseDetail>(`/api/builder/exercises/${id}`) })

export function useExerciseActions(courseId: number) {
  const qc = useQueryClient()
  const refresh = () => Promise.all([qc.invalidateQueries({ queryKey: ['builder', 'exercises', courseId] }), qc.invalidateQueries({ queryKey: ['builder', 'exercise'] })])
  return {
    create: useMutation({ mutationFn: (v: { typeKey: string; title: string }) => api<ExerciseDetail>(`/api/builder/courses/${courseId}/exercises`, { body: v }), onSuccess: refresh }),
    update: useMutation({
      mutationFn: (v: { id: number; title: string; config: ExerciseConfig; competencyIds: number[] }) =>
        api<ExerciseDetail>(`/api/builder/exercises/${v.id}`, { method: 'PUT', body: { title: v.title, config: v.config, competencyIds: v.competencyIds } }),
      onSuccess: refresh,
    }),
    remove: useMutation({ mutationFn: (id: number) => api(`/api/builder/exercises/${id}`, { method: 'DELETE' }), onSuccess: refresh }),
  }
}

// ---- learner

export const useExerciseView = (id: number) => useQuery({ queryKey: ['exercise', id], queryFn: () => api<ExerciseView>(`/api/exercises/${id}`) })

export const useExerciseAttempt = (id: number) => useQuery({ queryKey: ['exercise-attempt', id], queryFn: () => api<ExerciseAttempt>(`/api/exercise-attempts/${id}`) })

export const useMyExerciseAttempts = () => useQuery({ queryKey: ['exercise-attempts'], queryFn: () => api<ExerciseAttemptSummary[]>('/api/me/exercise-attempts') })

export function useSubmitExercise(exerciseId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (submission: Record<string, unknown>) => api<ExerciseAttempt>(`/api/exercises/${exerciseId}/attempts`, { body: { submission } }),
    onSuccess: () => Promise.all(['exercise', 'exercise-attempts', 'competencies'].map((k) => qc.invalidateQueries({ queryKey: [k] }))),
  })
}
