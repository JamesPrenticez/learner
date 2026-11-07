# learner

A modular professional training platform: a **Learner app** and a separate **Course Builder**, on one shared API.
The spec is [.docs/BUILD-PLAN.md](.docs/BUILD-PLAN.md). Git conventions are in [.docs/git-conventions.md](.docs/git-conventions.md).

## Run it

`pnpm install`, then `pnpm run dev` (frontend and API together).

| Part                  | URL                   | Command           |
| --------------------- | --------------------- | ----------------- |
| Frontend (React/Vite) | http://localhost:3000 | `pnpm run dev:fe` |
| API (ASP.NET Core)    | http://localhost:3001 | `pnpm run dev:be` |

In Development the API creates a SQLite database (`backend/src/Learner.Api/learner.db`) and seeds demo accounts and one sample course. All passwords are `Passw0rd!`.

| Role    | Email               |
| ------- | ------------------- |
| Learner | learner@example.com |
| Teacher | teacher@example.com |
| Admin   | admin@example.com   |

The schema is created with `EnsureCreated` (no migrations yet), so **delete `learner.db` after pulling a schema change**.

## Test

- `dotnet test backend` (unit and integration tests; each test class gets its own throwaway database)
- `pnpm --dir frontend lint` and `pnpm --dir frontend build`

CI (`.github/workflows/ci.yml`) runs lint, build and the format check on every PR to `master`.

## How it fits together

- **Learner app** (`/`) and **Course Builder** (`/builder`, teachers and admins only, lazy-loaded) are separate shells and route trees in one frontend, sharing the API.
- **Permissions are enforced on the server.** Course access goes through one class, `Access`; the UI only hides things.
- **Everything curriculum-specific is data.** Extending the platform means adding one registry entry per side:

| To add a…          | Frontend                    | API                         |
| ------------------ | --------------------------- | --------------------------- |
| Content block type | `src/blocks/registry.ts`    | `Features/BlockSchemas.cs`  |
| Question type      | `src/questions/registry.ts` | `Features/QuestionTypes.cs` |
| Exercise component | `src/exercises/registry.ts` | `Features/ExerciseTypes.cs` |

Exercises have a documented contract: [.docs/exercise-contract.md](.docs/exercise-contract.md).

- **History is never rewritten.** Quiz and exercise attempts store a snapshot of what the learner saw and how it was marked. Competency state is derived from stored evidence on every read, using rules in `appsettings.json` (`Competency`).
