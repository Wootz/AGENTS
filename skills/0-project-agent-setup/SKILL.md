---
name: 0-project-agent-setup
description: Create or update project-local AI development guidance and skills. Discover current skill sources, select those needed for the project's workflow, copy or merge them into .agents/skills while preserving project adaptations, and maintain AGENTS.md for Codex and current Claude Code. Use after scaffolding or for requested changes to project agent rules, local skills, or team workflow (「建立或更新專案 AGENTS.md」「複製或更新專案 skills」「團隊共用開發流程」).
---

# Project Agent Setup

Make a project usable by other developers without the original developer's global instructions or skill installation. Use this workflow both for initial setup and for requested updates to project guidance, local skills, or the agreed development workflow. It configures agent guidance without scaffolding application code or adding ongoing synchronization with global skills.

Each skill is independent. The user decides which skills the project adopts; this skill does not require a scaffolder, workflow-setup skill, or another skill to run first. Reuse existing project documents regardless of their producer.

Human-facing responses and documents use Traditional Chinese. Agent-facing files, including copied skills and `AGENTS.md`, use English.

## 1. Inspect the project and discover current skills

- Identify the workspace root, technology stack, agreed development workflow, relevant documentation, and actual build/test commands. Read existing `AGENTS.md`, `CLAUDE.md`, and agent rules before editing. Preserve project decisions; surface material conflicts together before changing them.
- Read [references/development-workflow.md](references/development-workflow.md) when recommending or changing workflow skills. It describes optional Spec and Plan choices; record only the skills and gates the user adopts for this project.
- For updates, inventory the installed project skills and read their local instructions and adaptations. Determine which guidance or skills the user wants updated; updating a subset does not authorize replacing the entire project skill tree.
- Discover skill sources from the current session's catalog and readable local skill directories. Resolve the user's home through the environment when needed; never assume a developer-specific absolute path. Ask for a source location only if it cannot be discovered.
- Enumerate the current source files on **every invocation** and read each candidate's actual `SKILL.md` frontmatter (`name`, `description`). A session catalog helps locate sources but may be incomplete or stale. Do not store a fixed skill list or embed copies of global skills in this skill's assets.
- Include hidden and Git-ignored installed skills when enumerating a source directory (for example, `rg --files --hidden --no-ignore -g SKILL.md <source-root>`). Source-control ignore rules do not determine whether a skill is installed.
- Match candidates to this project's stack and confirmed workflow. Include only skills developers will need for that workflow; creating a project does not justify copying the entire local library. Different projects may select different sets. For specification work, use `0-generate-spec`; exclude the owner's personal `to-spec` from project exports.
- Read selected skills in full, and inspect their referenced resources and helpers. Identify actual document, tool, and runtime requirements. Treat references to other workflows as optional recommendations, not mandatory skill dependencies. Keep necessary supporting documents/resources within the selected skill or project, and resolve missing resources before claiming the project is self-contained.

## 2. Confirm the project-specific selection

Present a compact table in Traditional Chinese: source skill name, project skill name, purpose in this project, source location, and required adaptation or document/tool prerequisites. Derive every row from the current source content and project needs; include no automatically required companion skills. For updates, compare the current source with the mapped project copy and show the proposed additions or updates, retained project adaptations, and any explicit removal proposals. A source that is no longer installed locally is not evidence that its project copy should be deleted.

Build a source-to-project name map before writing. Remove exactly one leading `0-` from every exported skill name; retain the rest of the name. Names without that prefix stay unchanged. For example, `0-generate-spec` becomes `generate-spec`, and `0-pm-05-generate-srs` becomes `pm-05-generate-srs`. Check the mapped names against the selected sources and existing project skills for collisions, including case-insensitive collisions on Windows. Reuse an existing mapped copy for an update only when it is the intended source's project copy; ask about ambiguous collisions before overwriting or merging.

Use an already-approved selection when present. Otherwise ask once about the proposed project-local selection and adaptations. Do not add another confirmation if the existing approval already covers them. Missing skills or incompatible prerequisites require a concrete alternative and a user decision, rather than silently installing or replacing tools.

Run this skill whenever the user requests project guidance setup or updates. Use the project's current state; no preceding skill stage is required.

## 3. Copy or update physical files and adapt the project copies

- Copy each selected skill's **current complete directory** into `<workspace>/.agents/skills/<project-skill-name>/` using the approved name map, retaining the resources its workflow needs (`SKILL.md`, scripts, references, assets, and metadata). Re-read the source before copying if it changed since selection. Do not reconstruct a skill from its name, a cached summary, or a template.
- Rename the project copy's directory and YAML `name` together. Update actual skill invocations (`$skill-name`, `/skill-name`), skill paths, cross-skill references, templates, and `agents/openai.yaml` fields that contain the old invocation/name. Apply the map across all selected copies and project guidance; change skill identifiers and paths without broadly replacing unrelated code or prose. Keep source/global names and files unchanged.
- Create ordinary files and directories. The generated guidance and skill tree must contain **no symlinks, junctions, or other filesystem links**, including links within copied resources. If a source uses links, copy the resolved contents as physical files; stop for broken links or cycles. Use copy operations available on the host OS; do not require Unix link commands or administrator privileges.
- Preserve existing project guidance and locally adapted skills. Compare and merge only the approved changes; do not overwrite unrelated content. Leave source/global skills untouched.
- For updates, merge relevant source changes into existing mapped project copies, including affected resources, instead of replacing the directory wholesale. Reapply project names to incoming source content so updates do not restore `0-` prefixes. If migrating an older prefixed project copy, move and merge it into the mapped directory while preserving its adaptations and repairing references. Apply the requested rule/workflow changes to their authoritative project documents and update dependent pointers. Preserve intentional project differences; ask about unresolved conflicts rather than assuming the source version should win. Remove skills or resources only when that removal is authorized, and repair their callers and index entries.
- Inspect copied instructions, references, scripts, and templates for developer-specific paths, home-directory rule dependencies, cross-skill links, product-specific tool requirements, and OS assumptions. Rewrite applicable paths to project-relative locations and resolve resource paths relative to the owning skill. Copy required external rule files into the project and update their callers.
- Adapt only the project-dependent details: namespaces, folder layout, documentation paths, package manager, selected architecture, and real validation commands. Preserve the skill's purpose, authorization boundaries, and required safeguards. Resolve conflicts with project contracts through the user before editing those contracts.
- In project copies, express needed inputs as document/data requirements rather than requiring another skill to produce them. Preserve necessary reference material while removing forced cross-skill execution or installation.
- For macOS, Windows, and WSL, document actual prerequisites and suitable commands. Prefer portable helpers where practical; existing Bash helpers may remain when their prerequisite and a supported Windows execution path are clear. Do not describe a Unix-only command as native PowerShell. If a selected workflow cannot run on a target platform, resolve the gap instead of claiming compatibility.

## 4. Create or update the project instruction entrypoint

Create or merge `AGENTS.md` at the **workspace root**. Use [assets/AGENTS.template.md](assets/AGENTS.template.md) as a starting point for a new file; update relevant sections of an existing file in place. Fill it from project facts and the applicable instructions; remove all template tokens from the result.

- Keep it an index and actionable policy. Preserve applicable language, security, documentation alignment, brownfield, dependency, and Git rules. Longer rule bodies can live under `docs/agents/` with explicit mandatory-reading conditions in `AGENTS.md`; dependency rules must retain their full constraints, not just a license summary.
- Record the confirmed workflow and its completion gates. A skill helps execute a stage; its presence does not authorize that stage, approve a specification, or permit commits/deployment.
- Preserve and integrate the existing `Agent skills` configuration block and its document pointers. Keep tracker, optional triage, and domain-consumption rules aligned with the project's actual configuration; update the local skill index alongside them without appending duplicate sections.
- State that project skills live in `.agents/skills/`, with a table of **only the installed skills**: actual name, when to use it, and project-relative `SKILL.md` path. Generate rows from the final project copies, not a preconfigured catalog.
- When using a project skill, require both Codex and Claude Code to read its `SKILL.md` and follow its conditional resource pointers. Use the mapped project name and prefer the project's adapted copy over its global source. Installing a skill does not mandate using it for every matching task; follow the user-adopted workflow. Do not load every skill body at startup.
- Target current Codex and Claude Code. New projects need no compatibility `CLAUDE.md`, tool-specific duplicate skill tree, symlink, or discovery setting. Claude Code can use the skill paths through `AGENTS.md`; this does not promise native slash commands for `.agents/skills/`.
- If the workspace already has a `CLAUDE.md` that prevents Claude Code from reading `AGENTS.md` directly, preserve its instructions and merge an `@AGENTS.md` import outside code fences as needed. This handles the existing project's entrypoint, rather than adding an old-version compatibility file.
- Ensure `AGENTS.md`, the selected `.agents/skills/` files, and required agent references are not excluded by project ignore rules. They must be available in a normal checkout. Do not commit or push them automatically.

## 5. Verify the handoff

Before declaring completion:

- Every approved skill has a readable `SKILL.md` with valid `name` and `description`; each table path and required resource exists inside the project.
- Each project directory matches its YAML `name` and the approved map; exported names have no leading `0-`. Check invocation/path references and metadata after renaming, including updates, and verify that `to-spec` is absent from the project export/index. Source-provenance mentions may retain original names, but active project invocations and skill paths must use the mapped names.
- Scan the generated guidance, skills, and required resources for filesystem links and remaining references to the original developer's home or global skill installation. Required workflows must work from the project copies without those sources.
- Check adapted helpers with representative inputs or non-mutating checks where meaningful. Review macOS, Windows, and WSL prerequisites explicitly; report any platform that could not be executed locally.
- Confirm the instruction entrypoint, actual workflow, local skill table, and required-reading conditions agree. Remove unresolved template tokens. Do not mark the handoff complete while required inputs or supporting resources are missing.
- Report the created/updated paths, selected skills, retained project adaptations, and validation limits in Traditional Chinese. Run this workflow again when a developer requests another update; it does not add automatic checks, background updates, or a synchronization service.
