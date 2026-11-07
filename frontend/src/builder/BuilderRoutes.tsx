import { Route, Routes } from 'react-router'
import { BuilderLayout } from './BuilderLayout'
import { CompetenciesTab } from './CompetenciesTab'
import { CourseIndexRedirect, CourseLayout } from './CourseLayout'
import { CoursesPage } from './CoursesPage'
import { DetailsTab } from './DetailsTab'
import { ExercisesTab } from './ExercisesTab'
import { LessonEditor } from './LessonEditor'
import { QuestionsTab } from './QuestionsTab'
import { QuizzesTab } from './QuizzesTab'
import { SharingTab } from './SharingTab'
import { StructureTab } from './StructureTab'

/** The Course Builder app. Lazy-loaded from App.tsx, so learners never download it. */
export default function BuilderRoutes() {
  return (
    <Routes>
      <Route element={<BuilderLayout />}>
        <Route index element={<CoursesPage />} />
        <Route path="courses/:courseId" element={<CourseLayout />}>
          <Route index element={<CourseIndexRedirect />} />
          <Route path="structure" element={<StructureTab />} />
          <Route path="questions" element={<QuestionsTab />} />
          <Route path="quizzes" element={<QuizzesTab />} />
          <Route path="exercises" element={<ExercisesTab />} />
          <Route path="competencies" element={<CompetenciesTab />} />
          <Route path="details" element={<DetailsTab />} />
          <Route path="sharing" element={<SharingTab />} />
          <Route path="lessons/:lessonId" element={<LessonEditor />} />
        </Route>
      </Route>
    </Routes>
  )
}
