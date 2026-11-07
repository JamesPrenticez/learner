import { lazy, Suspense } from 'react'
import { BrowserRouter, Route, Routes } from 'react-router'
import { RequireRole } from './components/RequireRole'
import { CataloguePage } from './learner/CataloguePage'
import { CompetenciesPage } from './learner/CompetenciesPage'
import { CoursePage } from './learner/CoursePage'
import { ExerciseResultPage } from './learner/ExercisePlayer'
import { Dashboard } from './learner/Dashboard'
import { LearnerLayout } from './learner/LearnerLayout'
import { LessonPage } from './learner/LessonPage'
import { AttemptPage, ResultsPage } from './learner/ResultsPage'
import { UsersPage } from './pages/admin/UsersPage'
import { AuthPage } from './pages/AuthPage'

// The builder is a separate app experience: its own shell and routes, loaded only for teachers/admins.
const BuilderRoutes = lazy(() => import('./builder/BuilderRoutes'))

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<AuthPage mode="login" />} />
        <Route path="/register" element={<AuthPage mode="register" />} />
        <Route
          path="/builder/*"
          element={
            <RequireRole roles={['Teacher', 'Admin']}>
              <Suspense fallback={<p>Loading…</p>}>
                <BuilderRoutes />
              </Suspense>
            </RequireRole>
          }
        />
        <Route
          element={
            <RequireRole>
              <LearnerLayout />
            </RequireRole>
          }
        >
          <Route index element={<Dashboard />} />
          <Route path="catalogue" element={<CataloguePage />} />
          <Route path="competencies" element={<CompetenciesPage />} />
          <Route path="exercise-results/:attemptId" element={<ExerciseResultPage />} />
          <Route path="results" element={<ResultsPage />} />
          <Route path="results/:attemptId" element={<AttemptPage />} />
          <Route path="courses/:courseId" element={<CoursePage />} />
          <Route path="courses/:courseId/lessons/:lessonId" element={<LessonPage />} />
          <Route
            path="admin/users"
            element={
              <RequireRole roles={['Admin']}>
                <UsersPage />
              </RequireRole>
            }
          />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}

export default App
