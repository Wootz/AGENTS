# Global Development & Documentation Guidelines

## 🌐 Language
All AI responses must be in **Traditional Chinese (正體中文)**.

## 🎯 General Principles
- ❌ **No Over-Engineering**: Prefer intuitive, minimal implementations. No redundant DTO mappings, no multi-layered abstractions for simple CRUD, no unnecessary design patterns. (Interfaces for DI and test mocking are encouraged.)
- 🚫 **No Gold-Plating**: Build exactly what was asked — no extra features, options, or configurability "for future needs". Don't design for requirements that don't exist yet. If something extra seems worth adding, suggest it instead of building it.
- 🛡️ **Credentials**: Never hardcode secrets. Inject via environment variables or secret managers.
- 📦 **Dependencies**: Commercially friendly licenses only (MIT, Apache 2.0); GPL-like copyleft is prohibited.
- 📄 **Pagination**: When designing new pagination, name the param `page` for **1-based** and `pageIndex` for **0-based**. If existing docs already specify the convention, follow those.
- 🔒 **Git**: Obtain explicit user approval before any `git commit` (including all variants); automatic commits are strictly forbidden. `git push` is strictly forbidden.
- ✍️ **Commit Message**: Contain only the change description itself. Never append AI/tool attribution trailers such as `Co-Authored-By`, `Claude-Session`, `Generated with ...`, or any similar footer.
- 📌 **TODO Marker**: Mark every pending task or open decision in code with a `TODO: xxxxx` comment.

## 📖 Docs-First Alignment
- Where documentation exists (local `/docs`, design specs, this file), it is the single source of truth: namespaces, DB schemas, API routes, and payload models must match the documented contracts.
- On any conflict between docs, code, and UI (including `page`/`pageIndex` mismatches): correct the documentation first, then implement to match — never silently patch the code.
- Where a design spec exists, the UI must mirror it exactly: layout, spacing, visual hierarchy, and component states (hover, focus, disabled).

## 📝 Documentation Style
- **Write results, not history**: By default, documents describe the final state only — readers need to know how things are, not how they got there. Change narratives (e.g., "because of X, changed to Y") belong in changelogs (異動紀錄), or wherever the user explicitly asks for the rationale or the process.
- **Technology choices are the exception**: Every technology selection must show the comparison — the candidates considered, the criteria, and why the chosen one won. A verdict without the trade-offs cannot be reviewed or revisited later.
- **Keep it short**: Be concise and to the point. Overly long documents get skipped, not read — prefer bullet points and tables over lengthy prose.

## 🏚️ Brownfield Projects
- 🔍 **Scan before you touch**: Read surrounding files and match existing patterns, naming, and architecture exactly — even where they differ from this document's defaults (e.g., a different ORM, folder structure, or framework version). Do not migrate or modernize without explicit instruction.
- 🔬 **Minimal footprint**: Change only what is requested. No refactoring, renaming, extraction, "clean up", or dependency/lock-file upgrades beyond the task's scope.

## ⚡ Conflicts & User Decisions
Never decide on the user's behalf. Ask whenever the requirement is unclear, several viable approaches exist, or the user's approach has potential errors or conflicts with existing patterns:
1. State the question or issue explicitly, and lay out the options or alternatives with reasoning.
2. Ask the user before proceeding — never silently guess, pick one, or work around it.
3. If the user chooses to proceed anyway, implement exactly as requested without further objection.

Don't overthink it: routine judgment calls with an obvious default are made, not asked about. Ask when the answer would genuinely change the work — not to confirm the trivial, and never the same question twice.

## 🎨 Frontend
- 📦 **Package manager**: Prefer `pnpm` for new projects. Existing projects keep whatever they already use; never switch package managers without explicit instruction.
