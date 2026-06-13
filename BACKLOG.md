# Huml.Net Backlog

## Where the backlog lives now

Active, tracked work for Huml.Net lives in two places:

- **[GitHub Issues](https://github.com/primeBeri/huml-dotnet/issues)** — bug reports and feature
  requests from users. This is where to report something or request a change.
- **`backlog/` (Backlog.md)** — the maintainer's structured task and milestone system
  (`backlog/tasks/*.md`, `backlog/milestones/*.md`). These files are the canonical record of
  accepted, in-progress, and completed work.

## How it works

- Users report bugs and request features via GitHub Issues.
- The maintainer triages issues and promotes accepted items into `backlog/` tasks, grouped under
  the milestones in `backlog/milestones/`.
- Items move through statuses: Planned → In Progress → Done, tracked in the task files.

## History

This file previously held a hand-maintained table of phase-numbered (`999.x`) items. That table
was retired in the 2026-06 documentation review: it had drifted out of sync with the `backlog/`
task system, referenced an internal `.planning/` review document that is not part of the public
repository, and used the pre-`0.2.0-beta.1` `Huml.*` facade name (now `HumlSerializer.*`). The
historical table remains available in the git history. Many of the bug items it listed were
resolved during the G3 security review — see the `### Security` section of
[CHANGELOG.md](CHANGELOG.md) and `docs/internals/g3-security-review.md`.
