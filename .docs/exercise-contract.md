# Exercise integration contract

An exercise component plugs into the platform without changes to the Course Builder, the content model, the assessment engine, the dashboard or the competency system. The framework owns everything generic; a component owns only what is specific to it.

Three things are kept apart (spec 8.2):

| Concept                    | What it is                                                                | Where it lives                                        |
| -------------------------- | ------------------------------------------------------------------------- | ----------------------------------------------------- |
| **Exercise component**     | The reusable interactive behaviour (e.g. contract review).                | `ExerciseType` (API) + `ExerciseTypeDef` (frontend)   |
| **Exercise configuration** | A teacher's scenario, materials, marking key and competency tags.         | `ExerciseConfiguration` row (`ConfigJson`, `Version`) |
| **Exercise attempt**       | One learner submission, its result, and the config it was marked against. | `ExerciseAttempt` row (immutable)                     |

## What a component provides

### Server (`backend/src/Learner.Api/Features/ExerciseTypes.cs`)

```csharp
new ExerciseType(
    Key:        "contract-review",                   // stable id stored with every configuration and attempt
    Label:      "Contract review",                   // shown in the builder's type picker
    Validate:   config => string? problem,           // is the configuration complete? null = ready
    ForLearner: config => JsonNode,                  // the configuration with the marking key removed
    Evaluate:   (config, submission) => ExerciseResult(score, maxScore, summary, details))
```

All three are pure functions over `JsonElement`. `details` is free-form JSON that the component's own Player renders. List the type in `ExerciseTypes.All`.

### Frontend (`frontend/src/exercises/registry.ts`)

```ts
{
  (type, label, defaults, Editor, Player);
}
```

- `Editor({ config, onChange })` is the authoring form. The builder shows whichever editor matches the selected type, so the builder never needs to understand the exercise.
- `Player({ config, submission, result, disabled, onSubmit })` is the learner experience. `config` is the already-redacted `ForLearner` output. Before submitting, `result` is undefined; after submitting (or when reviewing history) it carries `summary` and `details`, and the Player shows its own feedback.

## What the framework does for every component

- Stores configurations, bumps `Version` whenever `ConfigJson` changes, and exposes `Problem` (from `Validate`) so drafts can be saved incomplete.
- Blocks publishing a course whose exercise blocks point at a missing or incomplete exercise.
- Serves only `ForLearner(config)` to learners. The marking key never leaves the server before submission.
- Requires enrolment (or course authorship) to submit, and a ready configuration.
- On submit: runs `Evaluate`, stores an `ExerciseAttempt` holding the config snapshot, its version, the submission and the result, so a result can be reconstructed after the exercise is edited.
- Writes `CompetencyEvidence` (source `exercise`, weighted by `maxScore`) for every competency the exercise is tagged with. Evidence rules live in `appsettings.json` (`Competency:SourceWeights`).
- Refuses to delete an exercise that has attempts.

## Endpoints

| Who     | Endpoint                                                           |
| ------- | ------------------------------------------------------------------ |
| Builder | `GET /api/builder/exercise-types`                                  |
| Builder | `GET/POST /api/builder/courses/{id}/exercises`                     |
| Builder | `GET/PUT/DELETE /api/builder/exercises/{id}`                       |
| Learner | `GET /api/exercises/{id}` (redacted config)                        |
| Learner | `POST /api/exercises/{id}/attempts` `{ submission }`               |
| Learner | `GET /api/exercise-attempts/{id}`, `GET /api/me/exercise-attempts` |

## Adding a component, step by step

1. Write the `ExerciseType` (a static class like `ContractReview.cs`) and add it to `ExerciseTypes.All`.
2. Write its `Editor` and `Player` under `frontend/src/exercises/` and add one entry to the registry.
3. Done. Authoring, lesson embedding (the `exercise` block), attempts, history, competency evidence and the dashboard already work.

## Reference component: contract review

```jsonc
// config
{ "scenario": "...", "instructions": "...",
  "clauses": [{ "id": "c1", "title": "1. Payment", "text": "..." }],
  "issues":  [{ "clauseId": "c1", "explanation": "Why this is a problem", "points": 2 }] }   // issues = marking key, stripped by ForLearner

// submission
{ "flags": [{ "clauseId": "c1", "note": "optional" }] }
```

Score = points for each issue found, minus 1 for each flagged clause that has no issue, never below zero.

## Not yet supported

- Manual teacher marking of free-text exercises (the `RequiresReview` competency state is reserved for it).
- Recommending exercises (as opposed to lessons) for competency gaps.
- Multi-stage exercises are possible inside one Player (it owns its own stages), but each submission is a single attempt.
