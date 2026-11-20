# Engineering instructions

## Scope and communication

- Communicate with the developer in Romanian. Write project documentation in concise English Markdown.
- Require implementation prompts to contain fewer than 50 lines. If a future implementation prompt reaches 50 lines, request a shorter scoped prompt before implementation. This rule applies to future tasks, not retroactively to the task establishing this file.
- Inspect the repository and applicable instructions before editing. Never assume files, assets, tools, or completed features exist.
- Implement only authorized scope. Ask for approval before starting a new implementation task; explicit task authorization is sufficient for its necessary edits and checks.
- Do not modify unrelated files, initialize Unity, or install packages without explicit authorization.

## Project constraints

- Target Unity 6.3 LTS (`6000.3.27f1`), C#, URP, AR Foundation, ARCore, Android ARM64/IL2CPP, and Samsung Galaxy S21. Do not assume Editor installation is complete.
- Preserve the approved Presentation, Application, AR, and Content layers and planned Bootstrap, Museum, and ARExperience scenes. See [architecture](docs/architecture/ARCHITECTURE.md).
- Keep runtime operation offline: no backend, cloud services, accounts, remote content requirements, or online runtime dependencies.
- Keep historical metadata in JSON and Unity asset references in ScriptableObjects. Preserve identifiers and validate catalog changes.
- Retain one main prefab and one unique marker per exhibit, one active AR model, and user rotation/zoom across tracking loss. Treat 200 ms loss debounce as an initial target requiring measurement.
- Clearly distinguish implemented behavior, placeholders, and plans. Never fabricate historical facts, licenses, sources, installed tooling, or test results.
- Preserve `content/catalog/museum_catalog.json` and `scripts/Validate-Catalog.ps1` unless a future task explicitly authorizes their modification.

## Git and verification

- Never execute `git add`, `git commit`, `git push`, `git reset`, `git checkout`, `git clean`, or any other state-changing Git command. Never change Git configuration, index, refs, or history, including through alternative tools.
- Read-only Git inspection such as status, diff, and log is allowed. The human developer exclusively stages, commits, and pushes.
- Check Markdown structure, internal references, catalog consistency, and ignore coverage when changing documentation or repository conventions.
- Run the existing catalog validator in Draft mode when PowerShell is available and relevant. Report failures and warnings honestly; a Draft pass does not certify release readiness.
- Use [acceptance criteria](docs/design/ACCEPTANCE_CRITERIA.md) for future behavior verification. Record device/build conditions and evidence for runtime claims.
- After every task, report in Romanian: summary; every created, modified, and deleted file with repository-relative paths; verification results; outstanding issues; and exact repository-relative paths recommended for human `git add`. Explicitly state when no files were deleted or a check was not run.
- Default to a detailed implementation report unless the developer requests a shorter answer. Explain the resulting behavior, requirement coverage, architectural responsibilities, important implementation decisions, temporary states, and preserved scope. Distinguish implemented behavior, automated evidence, manual checks, and future work. Include actual errors/warnings and their resolution or remaining impact, exact evidence paths, limitations/risks, and concrete next verification steps. For large generated/imported file sets, group them by purpose and link a complete per-file inventory. Keep original implementation results separate from later reporting-only changes; never imply tests were rerun when only existing evidence was inspected.
