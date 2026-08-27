# Global Development & Documentation Guidelines

## 🌐 Language
All AI responses must be in **Traditional Chinese (正體中文)**.

## 🎯 General Principles
- ❌ **No Over-Engineering**: Prefer intuitive, minimal implementations. No redundant DTO mappings, no multi-layered abstractions for simple CRUD, no unnecessary design patterns. (Interfaces for DI and test mocking are encouraged.)
- 🛡️ **Credentials**: Never hardcode secrets. Inject via environment variables or secret managers.
- 📦 **Dependencies**: Latest **stable** versions only (no preview/beta). Commercially friendly licenses only (MIT, Apache 2.0); GPL-like copyleft is prohibited.
- 📄 **Pagination**: param `page` → **1-based**; param `pageIndex` → **0-based**.
- 🔒 **Git**: Obtain explicit user approval before any `git commit` (including all variants); automatic commits are strictly forbidden. `git push` is strictly forbidden.
- 📌 **TODO Marker**: Mark every pending task or open decision in code with a `TODO: xxxxx` comment.

## 📖 Docs-First Alignment
- Documentation (local `/docs`, design specs, this file) is the single source of truth. Namespaces, DB schemas, API routes, and payload models must match documented contracts.
- On any conflict between docs, code, and UI (including `page`/`pageIndex` mismatches): correct the documentation first, then implement to match — never silently patch the code.
- The UI must mirror the designated design specs exactly: layout, spacing, visual hierarchy, and component states (hover, focus, disabled).

## 📝 Documentation Style
- **Write results, not history**: Regular documents describe the final state only. Change narratives (e.g., "because of X, changed to Y") belong exclusively in changelogs (異動紀錄), never in other documents.
- **Keep it short**: Be concise and to the point. Overly long documents get skipped, not read — prefer bullet points and tables over lengthy prose.

## 🏚️ Brownfield Projects
- 🔍 **Scan before you touch**: Read surrounding files and match existing patterns, naming, and architecture exactly — even where they differ from this document's defaults (e.g., a different ORM, folder structure, or framework version). Do not migrate or modernize without explicit instruction.
- 🔬 **Minimal footprint**: Change only what is requested. No refactoring, renaming, extraction, "clean up", or dependency/lock-file upgrades beyond the task's scope.

## ⚡ Conflicts & User Decisions
When the user's approach has potential errors or conflicts with existing patterns:
1. Identify the issue explicitly and recommend alternatives with reasoning.
2. Ask the user before proceeding — never silently work around it.
3. If the user chooses to proceed anyway, implement exactly as requested without further objection.

## 🎨 Frontend
- 📦 **Package manager**: `pnpm` only, across the entire repository. `npm`/`yarn` are forbidden.
- 💅 **Styling**: Tailwind CSS exclusively. Utility classes follow Tailwind's recommended order (Layout → Flex/Grid → Spacing → Sizing → Typography → Visuals → Misc). Respect `tailwind.config.js` tokens; avoid arbitrary values like `bg-[#ff0000]` unless a design spec mandates it. No raw CSS/SCSS/CSS Modules except essential global stylesheets.
