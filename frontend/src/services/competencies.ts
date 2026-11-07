import { useQuery } from '@tanstack/react-query'
import type { CompetencyStatus, EvidenceRow } from '../types/competency'
import { api } from './api'

export const useMyCompetencies = () => useQuery({ queryKey: ['competencies'], queryFn: () => api<CompetencyStatus[]>('/api/me/competencies') })

/** The evidence behind one competency. Only fetched when the learner opens it. */
export const useEvidence = (id: number, enabled: boolean) =>
  useQuery({ queryKey: ['competencies', 'evidence', id], enabled, queryFn: () => api<EvidenceRow[]>(`/api/me/competencies/${id}/evidence`) })
