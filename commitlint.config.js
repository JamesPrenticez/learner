// Enforced by .husky/commit-msg. Slim Conventional Commits, see .docs/git-conventions.md

/** @type {import("@commitlint/types").UserConfig} */
export default {
  extends: ["@commitlint/config-conventional"],
  rules: {
    "type-enum": [2, "always", ["feat", "fix", "refactor", "docs", "chore", "test", "build", "ci", "perf", "revert"]],
    "type-case": [2, "always", "lower-case"],
    "type-empty": [2, "never"],
    "subject-empty": [2, "never"],
    "scope-empty": [0],
    "header-max-length": [2, "always", 72],
    "subject-case": [2, "never", ["sentence-case", "start-case", "pascal-case", "upper-case"]],
    "subject-full-stop": [1, "never", "."],
    "body-max-line-length": [0],
  },
};
