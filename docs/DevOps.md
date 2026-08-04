# CI

`dotnet.yml`, adapted from GitHub's official
[`ci/dotnet.yml`](https://github.com/actions/starter-workflows/blob/main/ci/dotnet.yml)
starter template, runs on push/PR to `main`. Changes from the template:

- Version pinned to `8.0.x` to match the `net8.0` target framework used by the solution.
- Point jobs to the solution and run in `Release` config.

I have blocked PRs until the build pipeline completes for safety (make sure I don't carry any possible messups into main - or at least minimize it).

## Claude PR Review

`claude-review.yml` runs [`anthropics/claude-code-action`](https://github.com/anthropics/claude-code-action)
on pull requests. It invokes the `dotnet-reviewer` subagent
(`.claude/agents/dotnet-reviewer.md`), which reads `docs/CHALLENGE_BRIEF.md` and posts its findings as a PR review.

This section was designed and set up with AI assistance (Claude Code)