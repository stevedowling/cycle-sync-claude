# CI workflow

[`ci.yml`](ci.yml) is the GitHub Actions pipeline for this repo. It lives here rather than under
`.github/workflows/` because the token used to open this PR lacks the `workflow` scope needed to add
workflow files.

## Install

Move it into place from a checkout with a `workflow`-scoped token (or via the GitHub web UI):

```bash
mkdir -p .github/workflows
git mv ci/ci.yml .github/workflows/ci.yml
git commit -m "Add CI workflow"
git push
```

## What it runs

Four jobs — none requires Docker-in-Docker; GitHub-hosted runners provide Docker at the host level
for the service containers and Playwright browsers:

| Job | What it does |
| --- | --- |
| `dotnet-acceptance` | Reqnroll BDD suite against in-memory SQLite (offline). |
| `web-unit` | SPA lint, Vitest component tests, production build. |
| `sql-integration` | Applies the EF migrations against a real SQL Server service container. |
| `e2e` | Playwright full-stack test (React → API → SQL Server) in the hermetic `E2E` environment. |
