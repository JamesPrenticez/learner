# learner

The spec goes in [.docs/BUILD-PLAN.md](.docs/BUILD-PLAN.md).

### Frontend (React + Vite)

http://localhost:3000
`pnpm run dev:fe`

### Backend (C#/.NET)

http://localhost:3001 (returns "Hello World!")
`pnpm run dev:be`

### Both

`pnpm run dev`

CI (`.github/workflows/ci.yml`) runs lint, build and format check on every PR to `master`.
