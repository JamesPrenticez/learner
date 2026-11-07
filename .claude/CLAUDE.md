# learner

React + C#/ASP.NET Core app. The spec lives in `.docs/BUILD-PLAN.md`. Follow its milestone order.

## Working style

- Claude builds this app: write the code, run it, test it.
- One milestone task at a time. Confirm the current task is working before moving on.
- Keep to the spec's scope. Flag scope creep; don't add abstractions speculatively.

## Git

- Follow `.docs/git-conventions.md`: commits are `<type>: <subject>` (enforced by husky + commitlint), branches are `<type>/<short-title>`, PRs target `master` (the branch is `master`, never `main`).
- Don't use `--no-verify`. If a hook fails, fix the cause.

## Layout

- `frontend/` — React + TS, Vite, pnpm (port 3000). TanStack Query for server state, Zustand for client-only state.
- `backend/` — `Learner.slnx`, `src/Learner.Api` (ASP.NET Core minimal API, port 3001, CORS origins in `appsettings.json`)

## Commands

- API: `dotnet run --project backend/src/Learner.Api`
- Backend build/test: `dotnet build backend` / `dotnet test backend`
- Frontend: `pnpm --dir frontend dev` / `pnpm --dir frontend build` / `pnpm --dir frontend lint`
- Both: `pnpm dev` from the repo root
- Prefer `rtk <cmd>` for token-efficient output (see global RTK.md).
