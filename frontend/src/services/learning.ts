import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { CatalogueFilters, CatalogueItem, CourseOverview, LessonView, MyCourse } from '../types/learning'
import { api } from './api'

export function useCatalogue(filters: CatalogueFilters) {
  const qs = new URLSearchParams()
  for (const [k, v] of Object.entries(filters)) if (v !== undefined && v !== '') qs.set(k, String(v))
  return useQuery({ queryKey: ['catalogue', filters], queryFn: () => api<CatalogueItem[]>(`/api/catalogue?${qs}`) })
}

export const useMyCourses = () => useQuery({ queryKey: ['my-courses'], queryFn: () => api<MyCourse[]>('/api/me/courses') })

export const useCourseOverview = (id: number) => useQuery({ queryKey: ['course', id], queryFn: () => api<CourseOverview>(`/api/courses/${id}`) })

export const useLesson = (id: number) => useQuery({ queryKey: ['lesson', id], queryFn: () => api<LessonView>(`/api/lessons/${id}`) })

/** Enrolment and completion change what several screens show, so refresh them all. */
function useRefreshing<V>(fn: (v: V) => Promise<unknown>) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () => Promise.all(['course', 'lesson', 'my-courses', 'catalogue', 'competencies'].map((k) => qc.invalidateQueries({ queryKey: [k] }))),
  })
}

export const useEnrol = (courseId: number) => useRefreshing(() => api(`/api/courses/${courseId}/enrol`, { method: 'POST', body: {} }))
export const useUnenrol = (courseId: number) => useRefreshing(() => api(`/api/courses/${courseId}/unenrol`, { method: 'POST', body: {} }))
export const useComplete = (lessonId: number) =>
  useRefreshing((completed: boolean) => api(`/api/lessons/${lessonId}/complete`, { method: 'POST', body: { completed } }))
