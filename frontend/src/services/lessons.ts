import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { BlockData, LessonEdit } from '../blocks/types'
import { api } from './api'

const key = (id: number) => ['builder', 'lesson', id]

export const useLessonEdit = (id: number) => useQuery({ queryKey: key(id), queryFn: () => api<LessonEdit>(`/api/builder/lessons/${id}`) })

/** Block operations for one lesson. Structural changes go straight to the server; text edits are saved in a batch with `saveAll`. */
export function useBlockActions(lessonId: number) {
  const qc = useQueryClient()
  const cache = (l: LessonEdit) => qc.setQueryData(key(l.id), l)
  return {
    add: useMutation({
      mutationFn: (v: { type: string; data: BlockData }) => api<LessonEdit>(`/api/builder/lessons/${lessonId}/blocks`, { body: v }),
      onSuccess: cache,
    }),
    remove: useMutation({ mutationFn: (id: number) => api<LessonEdit>(`/api/builder/blocks/${id}`, { method: 'DELETE' }), onSuccess: cache }),
    reorder: useMutation({
      mutationFn: (ids: number[]) => api<LessonEdit>(`/api/builder/lessons/${lessonId}/blocks/order`, { method: 'PUT', body: { ids } }),
      onSuccess: cache,
    }),
    /** Duplicate in this lesson (no target) or reuse in another one. */
    copy: useMutation({
      mutationFn: (v: { id: number; lessonId?: number }) => api<LessonEdit>(`/api/builder/blocks/${v.id}/copy`, { body: { lessonId: v.lessonId ?? null } }),
      onSuccess: cache,
    }),
    competencies: useMutation({
      mutationFn: (ids: number[]) => api<LessonEdit>(`/api/builder/lessons/${lessonId}/competencies`, { method: 'PUT', body: { ids } }),
      onSuccess: cache,
    }),
    saveAll: useMutation({
      mutationFn: (drafts: Record<number, BlockData>) =>
        Promise.all(Object.entries(drafts).map(([id, data]) => api(`/api/builder/blocks/${id}`, { method: 'PUT', body: { data } }))),
      onSuccess: (_, drafts) =>
        qc.setQueryData<LessonEdit>(key(lessonId), (l) => l && { ...l, blocks: l.blocks.map((b) => (drafts[b.id] ? { ...b, data: drafts[b.id] } : b)) }),
    }),
  }
}
