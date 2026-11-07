import { useOutletContext } from 'react-router'
import type { CourseDetail } from '../types/course'

export type CourseCtx = { course: CourseDetail }

/** The course loaded by CourseLayout, shared with every editor tab. */
export const useCourse = () => useOutletContext<CourseCtx>().course
