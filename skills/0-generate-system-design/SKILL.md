---
name: 0-generate-system-design
description: Write or update the project's system design specification (系統設計規格書) — an arc42-lite Markdown document at docs/architecture/ describing how the system is built now (context, building blocks, data, APIs, deployment, cross-cutting concepts, decisions). Use it at project closing to produce the deliverable (「結案」「產出系統設計規格書」「系統設計文件」), or mid-project to sync the document after a feature lands (「更新設計文件」「更新架構文件」「補設計文件」). Optional: it works with or without docs/specs, plans or ADRs.
---

# Produce the System Design Specification

The system design specification (系統設計規格書) is the closing deliverable that tells a reader who never opens the code how the system is built and why. Its structure follows **arc42**, keeping only the chapters this project needs.

It sits beside the other documents:

| Document | Answers | Lifetime |
| :--- | :--- | :--- |
| `docs/specs/` | What the system does and why (behavior) | Current state |
| `docs/architecture/` | How the system is built and why (structure) | Current state |
| `docs/plans/` | What this change will do | Disposable |
| `docs/adr/` | Why one decision was made | Append-only |

Every input except the code is optional. Small projects may have nothing but code and git history; the skill still runs, and asks the user more questions.

## Two kinds of content

The document's correctness depends on telling these apart:

- **Derived** — anything the code and config can show: modules, tables and columns, routes, deployment targets, environment variable names, packages. Write it from the code as it stands at this run. The code is the truth; never copy it from a plan or an older version of this document without checking.
- **Recorded** — anything the code is silent on: why a technology or design won and what was rejected, agreements with clients or external systems, constraints, quality targets, deliberate exceptions. Write it only from a **source**: an ADR, a spec, a plan, a commit message, this conversation, or the user's answer. Each recorded item cites its source. With no source, it becomes a question for the user (step 3), and an unanswered question becomes a `TODO:`.

Recorded content is what a later run cannot recover, so it is the reason to run this skill mid-project: decisions are still in the conversation and the plan.

## Output location

```
docs/architecture/
├── 系統設計規格書.md       # the document
└── modules/               # only when split (see below)
    └── {module}.md
```

`docs/design/` belongs to `0-figma-design-alignment` (UI design evidence); keep this document out of it.

**Split rule**: one file by default. When the project has roughly 5+ business modules (the same threshold `0-dotnet-project-setup` uses for `Modules/`), move each module's part of chapter 5 into `modules/{module}.md` (kebab-case of the module name) and leave a linking table in chapter 5. All other chapters stay in the main file.

## Steps

### 1. Choose the mode and scope

- **Full** — the document does not exist yet, or the user is closing the project. Scope: the whole codebase and every chapter. At closing, always run full, even if the document exists: it is the final reconciliation.
- **Incremental** — the document exists and the user wants it synced mid-project. Scope: changes since the commit recorded in the last 異動紀錄 row (`git diff <commit>..HEAD` plus uncommitted changes), mapped to the chapters they touch.

**Done when** the scope is written down as a list of chapters (and modules, if split) to write, or the run concludes there is no structural change (pure refactor, bug fix that restores documented behavior) and stops with that report.

### 2. Gather sources

Read, for the scope:

1. **Code and config** — project files, entry points, persistence configuration, endpoint definitions, deployment scripts, `appsettings*.json` / `.env.example` / CI files. Read config for **names and shapes only**; secrets and connection strings never enter the document.
2. **Existing records**, whichever exist: `docs/specs/`, `docs/plans/`, `docs/adr/`, `GLOSSARY.md`, SRS or requirement files (including `docs/sow/`, `docs/srs/`, `docs/change-requests/` when the project has them), `README.md`, the existing system design document, `git log` messages, this conversation.

Where a plan disagrees with the code, the code wins for structure; note each gap for the delivery report.

**Stop** if the code's behavior contradicts `docs/specs/`. That is a defect or an outdated spec, not a design question. Report it and let the user decide which to fix (AGENTS.md → Docs-First Alignment); keep it out of this document.

**Done when** every chapter in scope has its derived sources read and its recorded sources found or confirmed absent.

### 3. Ask once

Collect every recorded item in scope that has no source: missing rationale, unknown external agreements, unstated constraints or quality targets. Ask them in **one batch**, each as a concrete question, with your best guess when the code suggests one ("Was PostgreSQL chosen over SQL Server for licensing cost?"). A guess is a question, not an answer: it enters the document only once the user confirms it.

Whatever the user cannot or does not answer becomes `TODO: <the specific question> — <who decides>` in the document.

### 4. Write

Follow [`references/template.md`](references/template.md). New document: create it from the template. Existing document: edit in place.

- Describe the current state only. Delete what no longer holds; changes go only in 異動紀錄 (AGENTS.md → Documentation Style).
- Technology choices show candidates, criteria and the winner (AGENTS.md → Documentation Style). When an ADR holds the comparison, summarize in one line and link it.
- A chapter with no content in this project stays as its heading plus `本系統不適用：<理由>`, so a reviewer sees it was considered, not forgotten.
- Diagrams are Mermaid.
- Add one 異動紀錄 row: date, mode, the `HEAD` commit hash this run reflects, sources used, chapters changed. The next incremental run diffs from that hash.

**Done when** every chapter and module in the scope from step 1 is written or marked 不適用, and every recorded item from steps 2–3 sits in a chapter or a `TODO:`.

### 5. Verify against the code

Before delivering, check every derived fact you wrote in this run against the code once more: each table, column, route, module, environment variable and deployment target named in the document exists under that name. Check that every recorded item cites a source and that no secret value appears.

**Done when** each check passes or its fix is applied.

### 6. Deliver

Tell the user:

- The files written and the chapters changed
- Every `TODO:` added, listed for answering
- Every plan-versus-code gap found
- Any spec-versus-code contradiction (from step 2) awaiting their decision

Do not commit.
