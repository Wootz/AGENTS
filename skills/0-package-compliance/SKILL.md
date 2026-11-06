---
name: 0-package-compliance
description: Check a project's packages (NuGet, npm, pnpm, including transitive dependencies) against AGENTS.md → Dependencies — license, latest LTS/stable, 7-day release age, active maintenance, main-framework major. Run it after adding or upgrading any package, and whenever the user asks whether packages are compliant, whether a license is commercially usable, or to audit dependencies/licenses. Report-only: it never changes the project.
---

# Package Compliance Check

The single source of truth for the rules is `~/.agents/AGENTS.md` → Dependencies. The script's thresholds (7 days, 12 months, `minimumReleaseAge` 10080 minutes) and its list of packages released in lockstep with .NET live at the top of `scripts/check_packages.py`; when the rules change, change both.

This skill produces a report and stops there. Version changes, package swaps and config edits wait for the user's approval: a package change affects the whole project, and the user may have reasons the report cannot see (a client-mandated version, a purchased commercial license).

## Step 1: Run the checker

```bash
python3 -I ~/.agents/skills/0-package-compliance/scripts/check_packages.py <project-root>
```

- Needs network access: nuget.org, registry.npmjs.org, and the .NET / Node.js release indexes.
- .NET: runs `dotnet restore` (writes only `obj/`), then `dotnet list package --include-transitive` for the resolved versions.
- npm / pnpm: reads the lockfile directly (`pnpm-lock.yaml` v6+, `package-lock.json` v2+); `node_modules` is not needed.
- Exit code: `0` no violations, `1` violations found, `2` could not check.

Done when: the report exists and every item under "Could not check" is resolved or carried into the reply. An item that could not be checked has not passed.

## Step 2: Resolve what the script cannot judge

The script judges only SPDX licenses and registry data. Turn every ⚠️ row into a concrete conclusion:

- **License needs manual review**: open the license file or URL from the report and summarize three things: whether commercial use is allowed, whether it is dual-licensed or has a revenue threshold (e.g. FluentAssertions v8+, QuestPDF), and what restrictions apply. Whether to adopt it is always the user's call.
- **Inferred from license URL**: a legacy `licenseUrl` was mapped to an SPDX id; open the URL and confirm the mapping.
- **Weak copyleft (LGPL, MPL 2.0)**: verify each condition — dynamically referenced, unmodified, not merged into a single file:
  - .NET: search csproj files, `Directory.Build.props` and publish profiles for `PublishSingleFile`, `PublishAot`, ILMerge / Costura; confirm there is no modified copy of the package's source.
  - Frontend: a `dependencies` package that application code imports is inlined by the bundler and is therefore non-compliant; a devDependency used only at build time is never distributed and is unaffected.
  - Native libraries (e.g. sharp's libvips) load as a separate `.so` / `.dll`, which satisfies "separate library".
- **Package not found in the registry**: usually a private feed; ask the user for its license and version information.
- **npm upgrade across a major**: compare the `peerDependencies` listed in the report with the project's main packages (vue, react, vite, …). When they are incompatible, suggest the latest version within the current major instead.
- **Transitive findings**: find the direct package that brings it in — `dotnet nuget why <project> <package>`, `pnpm why <package>`, `npm explain <package>`. The fix is usually to upgrade or replace that direct package.

## Step 3: Report to the user

1. One-line verdict: all compliant, or N violations and N items needing the user's decision.
2. A table of violations and review items: package | problem | suggested fix (target version, alternative package, or removal). Mark packages that the current change added or upgraded.
3. A short list of ℹ️ suggestions; existing packages are not upgraded unasked (AGENTS.md → Brownfield).
4. Ask which fixes to apply, and wait for the answer before changing anything.
