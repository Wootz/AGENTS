# Global Development & Documentation Guidelines

## 🌐 Language
All AI responses must be in **Traditional Chinese (正體中文)**.
- **Human-facing output** — documents, reports, doc templates, code comments, and script messages that a skill or task produces — is in Traditional Chinese.
- **Agent-facing files** — skills (`SKILL.md`, their references and own scripts), `AGENTS.md` / `CLAUDE.md` — are in English. A skill description may add Chinese trigger keywords.

## 🎯 General Principles
- 🥇 **Priorities**: Security > Readability > Performance. Never trade security for anything; trade readability for performance only for a measured bottleneck. A security issue found outside the current task is reported and asked about, not fixed in passing.
- ❌ **No Over-Engineering**: Prefer intuitive, minimal implementations. No redundant DTO mappings, no multi-layered abstractions for simple CRUD, no unnecessary design patterns. (Interfaces for DI and test mocking are encouraged.)
- 🚫 **No Gold-Plating**: Build exactly what was asked — no extra features, options, or configurability "for future needs". Don't design for requirements that don't exist yet. If something extra seems worth adding, suggest it instead of building it.
- 🛡️ **Credentials**: Never hardcode secrets. Inject via environment variables or secret managers.
- 📦 **Dependencies**:
  - **Scope**: These rules apply to packages being added or upgraded. Existing packages are not upgraded unless the user asks (see Brownfield).
  - **Version**: The latest **LTS** release where one exists (frameworks/runtimes such as .NET, Node.js); otherwise the latest stable release. Never preview/beta/RC.
  - **Release age**: A version must have been published for at least **7 days** before it is adopted (supply-chain safety); for pnpm, enforce it with `minimumReleaseAge`.
  - **Actively maintained**: A release within the last 12 months, and not marked deprecated/archived.
  - **Main-package constraint**: Dependent packages follow the major version of the main package/framework they belong to — e.g., on .NET 8, use the latest stable `8.x.x` even if `10.x.x` exists.
  - **License**: Any commercially usable license (MIT, Apache 2.0, BSD, ISC, etc.); GPL-like copyleft is prohibited. Re-check the license of the exact version on every add or upgrade — licenses change between versions. A custom/proprietary, dual or revenue-threshold license that permits commercial use requires asking the user before adding it.
  - **Weak copyleft (LGPL, MPL 2.0)**: Allowed only when dynamically referenced as a separate library, unmodified, and not merged into a single file/bundle (e.g., `PublishSingleFile`, Native AOT, ILMerge, a frontend bundler inlining it).
  - **Transitive dependencies**: The license rules apply to the whole dependency tree, not only direct packages.
  - **Compliance check**: After adding or upgrading any package, run the `0-package-compliance` skill and report its findings.
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
- 🔬 **Minimal footprint**: Change only what is requested. No refactoring, renaming, extraction, "clean up", or dependency/lock-file upgrades beyond the task's scope. The "latest LTS/stable" dependency rule covers only packages newly added for the task.

## ⚡ Conflicts & User Decisions
Never decide on the user's behalf. Ask whenever the requirement is unclear, several viable approaches exist, or the user's approach has potential errors or conflicts with existing patterns:
1. State the question or issue explicitly, and lay out the options or alternatives with reasoning.
2. Ask the user before proceeding — never silently guess, pick one, or work around it.
3. If the user chooses to proceed anyway, implement exactly as requested without further objection.

❓ **Questions are not instructions**: When the user asks a question (e.g., "Is X allowed?", "Why does Y happen?"), only answer it — do not edit files, run changes, or apply fixes. Offer the change and wait for the user to ask for it.

Don't overthink it: routine judgment calls with an obvious default are made, not asked about. Ask when the answer would genuinely change the work — not to confirm the trivial, and never the same question twice.

## 🎨 Frontend
- 📦 **Package manager**: Prefer `pnpm` for new projects. Existing projects keep whatever they already use; never switch package managers without explicit instruction.
