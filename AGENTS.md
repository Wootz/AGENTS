# Global Development & Documentation Guidelines

## 🌐 Language
All AI responses must be in **Traditional Chinese (正體中文)**.

## 🎯 General Principles
- ❌ **Absolute Prohibition of Over-Engineering**: Prioritize intuitive, clean, and maintainable implementations. Avoid redundant DTO mappings, turning simple CRUD workflows into multi-layered abstractions, or enforcing unnecessary design patterns. *(Note: Interfaces for Dependency Injection and Test Mocking are necessary and encouraged.)*
- 🛡️ **Security & Performance**: Security first, performance second. Never hardcode sensitive credentials. Inject them via environment variables or secret managers.
- 🧱 **Design Principles**: Adhere to SOLID principles. Ensure code is readable, maintainable, and structurally consistent across the entire codebase.
- 📦 **Third-Party Dependencies**: Always use the latest **stable** versions (NO preview/beta). Use commercially friendly licenses only (e.g., MIT, Apache 2.0). GPL-like copyleft licenses are strictly prohibited.
- 📄 **Pagination Standards**:
  - Param named `page`: Implement **1-based** indexing (Page 1 = 1).
  - Param named `pageIndex`: Implement **0-based** indexing (Page 1 = 0).

## 📖 Three-Dimensional Alignment (Code, API, & UI/UX)
- � **Docs-First**: Documentation (local `/docs`, design specs, this file) is the single source of truth. Before generating code, scan local specification documents; namespaces, DB schemas, API routes, and payload models must strictly match defined contracts. When any conflict, defect, or inconsistency (including a pagination `page`/`pageIndex` mismatch) is found between docs, code, and UI/UX, **do NOT blindly modify the code** — correct the documentation first, then implement to match it.
- 🎨 **UI/UX & Design Alignment**: The implemented user interface must perfectly mirror the designated UI/UX design specifications, layouts, and wireframes. Visual hierarchy, component states (hover, focus, disabled), and spacing systems must be strictly identical across all views.
- ⛓️ **End-to-End Consistency**: Code design patterns, Web API contracts, and Frontend UI/UX presentation styles must remain entirely cohesive across the stack.

## 🏚️ Brownfield Project Guidelines
- 🔍 **Scan Before You Touch**: Before writing any code, read the surrounding files, existing patterns, naming conventions, and architectural decisions. Match them exactly — do not introduce a new style inconsistent with the codebase.
- 🚫 **No Unsolicited Refactoring**: Only change what is explicitly requested. Do NOT restructure, rename, extract, or "clean up" code that was not part of the task.
- 🧩 **Preserve Existing Patterns**: If the project uses a pattern that differs from the standards in this document (e.g., a different ORM, a different folder structure, a, even where they differ from this document's defaults (e.g., a different ORM, folder structure, or framework version) — do not migrate, modernize, or introduce a new style without explicit instruction.
- 🔬 **Minimal Footprint**: Change only what is explicitly requested. Do NOT restructure, rename, extract, "clean up" code, or upgrade dependencies/lock files beyond the task's scope. Avoid side effects on unrelated files or modules.
- ⚠️ **Declare Conflicts, Don't Resolve Silently**: If the requested change conflicts with existing code or architecture, report it to the user before proceeding. Never silently work around it
- 🔒 **Mandatory Git Commit Confirmation**: Before executing any `git commit` command, you **MUST** obtain explicit user approval. Automatic commits are strictly forbidden. This applies to all commit-related operations including but not limited to `git commit`, `git commit --amend`, `git commit -a`, and `git commit -m`. Violating this rule is considered a critical breach.

## ⚡ User-Driven Decision Making
- 🤝 **Conflict & Error Handling**: If the user's proposed implementation approach, design decision, or idea contains potential errors, conflicts with existing patterns, or has concerns:
  1. **Clearly identify and document** the issue, conflict, or potential problem.
  2. **Provide recommendations** with reasoning and alternative approaches.
  3. **Ask the user** for clarification or approval before proceeding.
  4. **Honor user decisions**: If the user chooses to proceed despite concerns, implement exactly as requested without further objection or automatic correction.
  5. **No silent workarounds**: Never silently modify the user's approach to avoid the conflict. Always raise concerns explicitly.

## 🎨 Package Management & Frontend Enforce
- 📦 **Package Manager**: Regardless of the frontend framework used (Vue, React, etc.) or fullstack workspace setups, **the entire repository environment MUST enforce `pnpm`**. Usage of `npm` or `yarn` is strictly forbidden.
- 💅 **Styling Standard (Tailwind CSS)**: **All frontend styling MUST standardise exclusively on Tailwind CSS**. 
  - Inline utility classes must follow Tailwind's official recommended order (Layout ➡️ Flex/Grid ➡️ Spacing ➡️ Sizing ➡️ Typography ➡️ Visuals ➡️ Misc).
  - Component design must respect the definitions inside `tailwind.config.js` (e.g., custom colors, spacing scale, semantic tokens). Writing arbitrary inline values (e.g., `bg-[#ff0000]`) is highly discouraged unless explicitly mandated by specific edge-case design specifications.
  - Raw/Vanilla CSS, SCSS/SASS, or CSS Modules are prohibited unless dealing with essential global stylesheet configurations.