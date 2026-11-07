import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { AccessKind, AccessRow, CourseDetail, CourseSummary, CourseUpdate, Curriculum } from '../types/course'
import { api } from './api'

export const useCurricula = () => useQuery({ queryKey: ['curricula'], queryFn: () => api<Curriculum[]>('/api/curricula') })

export function useAddCurriculum() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (title: string) => api<Curriculum>('/api/builder/curricula', { body: { title, description: '' } }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['curricula'] }),
  })
}

export const useBuilderCourses = () => useQuery({ queryKey: ['builder', 'courses'], queryFn: () => api<CourseSummary[]>('/api/builder/courses') })

export function useCreateCourse() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (title: string) => api<{ id: number }>('/api/builder/courses', { body: { title } }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['builder', 'courses'] }),
  })
}

export const useBuilderCourse = (id: number) =>
  useQuery({ queryKey: ['builder', 'course', id], queryFn: () => api<CourseDetail>(`/api/builder/courses/${id}`) })

/** Mutation whose response is the refreshed course; caches it so every tab of the editor updates. */
function useDetailMutation<V>(courseId: number, fn: (v: V) => Promise<CourseDetail>) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: (detail) => {
      qc.setQueryData(['builder', 'course', courseId], detail)
      qc.invalidateQueries({ queryKey: ['builder', 'courses'] })
    },
  })
}

export function useCourseActions(id: number) {
  const post = (path: string, body: unknown = {}) => api<CourseDetail>(path, { body })
  const put = (path: string, body: unknown) => api<CourseDetail>(path, { method: 'PUT', body })
  const del = (path: string) => api<CourseDetail>(path, { method: 'DELETE' })
  return {
    update: useDetailMutation(id, (b: CourseUpdate) => put(`/api/builder/courses/${id}`, b)),
    publish: useDetailMutation(id, () => post(`/api/builder/courses/${id}/publish`)),
    unpublish: useDetailMutation(id, () => post(`/api/builder/courses/${id}/unpublish`)),
    archive: useDetailMutation(id, () => post(`/api/builder/courses/${id}/archive`)),
    addModule: useDetailMutation(id, (title: string) => post(`/api/builder/courses/${id}/modules`, { title })),
    renameModule: useDetailMutation(id, (v: { id: number; title: string }) => put(`/api/builder/modules/${v.id}`, { title: v.title })),
    deleteModule: useDetailMutation(id, (moduleId: number) => del(`/api/builder/modules/${moduleId}`)),
    reorderModules: useDetailMutation(id, (ids: number[]) => put(`/api/builder/courses/${id}/modules/order`, { ids })),
    addLesson: useDetailMutation(id, (v: { moduleId: number; title: string }) => post(`/api/builder/modules/${v.moduleId}/lessons`, { title: v.title })),
    renameLesson: useDetailMutation(id, (v: { id: number; title: string }) => put(`/api/builder/lessons/${v.id}`, { title: v.title })),
    deleteLesson: useDetailMutation(id, (lessonId: number) => del(`/api/builder/lessons/${lessonId}`)),
    reorderLessons: useDetailMutation(id, (v: { moduleId: number; ids: number[] }) => put(`/api/builder/modules/${v.moduleId}/lessons/order`, { ids: v.ids })),
  }
}

export const useCourseAccess = (id: number) =>
  useQuery({ queryKey: ['builder', 'access', id], queryFn: () => api<AccessRow[]>(`/api/builder/courses/${id}/access`) })

export function useAccessActions(id: number) {
  const qc = useQueryClient()
  const refresh = () => qc.invalidateQueries({ queryKey: ['builder', 'access', id] })
  return {
    add: useMutation({
      mutationFn: (v: { email: string; kind: AccessKind }) => api<void>(`/api/builder/courses/${id}/access`, { body: v }),
      onSuccess: refresh,
    }),
    remove: useMutation({
      mutationFn: (userId: string) => api<void>(`/api/builder/courses/${id}/access/${userId}`, { method: 'DELETE' }),
      onSuccess: refresh,
    }),
  }
}
