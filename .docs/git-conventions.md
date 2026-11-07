# Git Conventions

Slim Conventional Commits, enforced by the `commit-msg` hook (commitlint). A `pre-commit` hook formats staged files (lint-staged + Prettier).

## Commit messages

```
<type>: <subject>

[optional body]

[optional footer]
```

Optional scope: `<type>(<scope>): <subject>`, e.g. `feat(api): add item list endpoint`.

| Type              | Use for                          |
| ----------------- | -------------------------------- |
| `feat`            | New feature or capability        |
| `fix`             | Bug fix                          |
| `refactor`        | Neither a fix nor a feature      |
| `test`            | Adding or updating tests         |
| `docs`            | Documentation only               |
| `chore`           | Tooling, dependencies, config    |
| `build` / `ci`    | Build system / GitHub Actions    |
| `perf` / `revert` | Performance / reverting a commit |

Rules:

- Imperative mood: "add item list endpoint", not "added" or "adds".
- Subject starts lowercase, no trailing full stop, header max 72 characters.
- Breaking change: `feat!: ...` or a `BREAKING CHANGE:` footer.

```
feat: add create item endpoint
fix: reject negative quantities
test: cover duplicate submissions
chore: add husky and commitlint
```

## Branches

Format: `<type>/<short-title>` with the same types as commits and a kebab-case title.

```
feat/create-item-endpoint
fix/quantity-validation
chore/git-hooks
```

- `master` is the only long-lived branch. Don't commit to it directly.
- Branch from `master`, open a PR back into `master`, delete the branch after merge.
- Suggested rhythm: one branch per milestone task, one PR per branch.

## Pull requests

- Title follows the commit format, e.g. `feat: add create item endpoint`.
- Description: what changed and why, how it was tested, screenshots for UI changes.
- CI must be green before merging.
