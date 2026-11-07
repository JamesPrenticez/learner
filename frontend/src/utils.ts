import type { CourseStatus } from './types/course'

/** Returns `ids` with the item at `from` removed and re-inserted at `to`. */
export function moveTo(ids: number[], from: number, to: number): number[] {
  const next = [...ids]
  next.splice(to, 0, next.splice(from, 1)[0])
  return next
}

export const statusTone = (s: CourseStatus) => (s === 'Published' ? 'success' : s === 'Draft' ? 'warn' : 'danger')
