---
name: 0-generate-plan
description: Produce a Spec Kit–format implementation plan and task list (Technical Context + data model + Tasks grouped by User Story) from an approved Spec, saved under docs/plans/ (「產出 Plan」「寫實作計畫」「拆任務」). Stage three of the SRS → Spec → Plan → Code → Verify flow; the Plan and Tasks live in one document.
---

# Produce a Plan (Implementation Plan and Tasks)

A Plan answers **how the behavior defined in the Spec is realized in this codebase**, expanded directly into an executable task list. It is disposable — once implemented it can be discarded, because the code itself is the most accurate description of the implementation.

It uses the **GitHub Spec Kit** format, with plan and tasks merged into one document.

> **Language**: the Plan is written in Traditional Chinese (正體中文), as in the template below. Keep technical identifiers (classes, methods, file paths) and the ID prefixes (`T` `US` `FR` `SC`) in English.

## Step 1: Confirm the Spec is approved

The Plan derives from the Spec. Before starting, confirm:

- The matching Spec exists under `docs/specs/`
- The user has reviewed and approved it
- **No `[NEEDS CLARIFICATION]` markers remain in the Spec**

The third point matters most. Entering the Plan with open questions builds the implementation on premises that will change. If markers remain, go back to the Spec stage for answers first.

**Exception**: pure engineering decisions (database migration strategy, external integration mechanics, cross-cutting infrastructure) have no external business behavior, should never have had a Spec, and can be planned directly.

## Step 2: Read the inputs

The plan rests on exactly two sources; bring in no others and assume no project conventions from memory:

1. **The matching Spec** — its User Stories, FRs and SCs are the target
2. **The same patterns in existing code** — find similar features already implemented in the codebase and follow them

Point 2 means a new feature should look like the existing ones rather than invent its own approach. Name the existing files the plan follows. If the codebase is brand new with no pattern to follow, state the structure you chose and why in the Plan, for the user to rule on at review.

## Document structure

```markdown
# 實作計畫：[功能名稱]

**日期**：[YYYY-MM-DD]
**Spec**：[對應的 docs/specs/ 路徑]

## 摘要

[從 Spec 擷取主要需求 + 本次採用的技術方向，三到五句]

## 技術脈絡

**語言 / 版本**：[依專案現況填寫]
**主要相依**：[專案既有的框架與函式庫]
**儲存**：[資料庫 / 檔案 / 外部服務]
**測試**：[專案使用的測試工具]
**專案類型**：[Web API / SPA / CLI / …]
**效能目標**：[對應 Spec 的 SC，若有]
**限制**：[對應 Spec 的 SC 或既有約束]

## 專案結構

### 本次異動範圍

\```text
[列出實際會新增或修改的目錄與檔案]
\```

## 資料模型 *(涉及資料時才寫)*

[實體、值物件、列舉；欄位與型別；關聯與索引]

## 對外契約 *(有對外介面時才寫)*

[端點路由、request / response、權限、錯誤碼]

## 交易邊界 *(涉及多筆寫入時才寫)*

[哪些寫入必須落在同一個交易內]

## Spec 對照

| Spec 項目 | 由誰實作 | 由誰驗證 |
| :--- | :--- | :--- |
| FR-001 | [檔案 / 元件] | [測試] |
| SC-001 | [實作方式] | [量測方式] |

## 第三方套件評估 *(要新增套件時才寫)*

| 套件 | 版本 | 授權 | 用途 | 為何不自己寫 |
| :--- | :--- | :--- | :--- | :--- |
| [名稱] | [版本] | [MIT / Apache-2.0 …] | [解決什麼問題] | [自製成本或風險] |

**授權**：[逐一說明是否允許商用與閉源散布]
**維護狀況**：[最後更新、下載量、未修補的已知漏洞]
**資安**：[已知 CVE 與影響版本；供應鏈風險]

---

# 任務

## Phase 1：基礎建設（共用）

- [ ] T001 [具體任務，含檔案路徑]

## Phase 2：前置相依（阻塞）

**⚠️ 本階段完成前，任何 User Story 都不能開始**

- [ ] T00X [P] [具體任務]

**檢查點**：基礎就緒，User Story 可開始

---

## Phase 3：User Story 1 - [標題] (Priority: P1) 🎯 MVP

**目標**：[這個故事交付什麼]
**獨立測試方式**：[取自 Spec 的同名欄位]

### 測試

- [ ] T0XX [P] [US1] [測試任務，含檔案路徑]

### 實作

- [ ] T0XX [P] [US1] [實作任務，含檔案路徑]

**檢查點**：User Story 1 應可獨立運作並通過驗收

---

## Phase N：收尾與跨切面

- [ ] TXXX [文件、效能、資安強化]

## 相依與執行順序

### 階段相依
### User Story 相依
### 可平行處理的部分
```

## Task-writing rules

### Format: `[ID] [P?] [Story] description`

- **[P]** — can run in parallel (different files, no dependencies)
- **[Story]** — the User Story it belongs to (US1, US2…), for tracing
- The description **must include the exact file path**

Tasks must be concrete enough to start on directly. 「實作訂單查詢」 fails; 「在 `src/[模組路徑]/GetOrdersQueryHandler` 實作分頁查詢，回傳欄位依對外契約」 passes — paths and names follow the project's existing structure.

### Group by User Story, not by technical layer

This is the core of Spec Kit task organization and the easiest thing to get wrong.

The instinct is to finish all data-layer work, then all service-layer work, then all interface-layer work — but then nothing runs until the end. Grouping by User Story instead makes every completed Phase an independently testable, demonstrable increment — P1 done is the MVP.

The exceptions are Phase 1 (infrastructure) and Phase 2 (prerequisites): they hold what **every Story needs** (shared base, database connection setup, validation framework). The test is "without this, no Story can start". Putting too much into Phase 2 turns it into disguised waterfall.

### Test tasks

Follow the project's existing test tools and conventions. Test tasks map to the Spec's **acceptance scenarios** — every acceptance scenario should have a matching test task; a missing one is a gap in the plan. Outbound HTTP in unit tests must be mocked or stubbed.

## Stay consistent with the existing architecture

Invent no new architecture rules in the Plan. Layering, naming, error handling and transaction handling all follow the codebase's existing patterns, and the Plan names the existing files it follows.

If the design **must** deviate from existing patterns (the existing structure cannot support the new requirement), mark the deviation and its reason explicitly in the Plan and raise it at delivery for the user to decide — never introduce a new style silently.

## Design principle: no over-engineering

The Plan is the stage most prone to growing extras — this is where "architecture" thinking starts, and architecture is easy to overthink.

**The test: every class, interface and abstraction layer to be created must point to the FR that needs it.** If it cannot, delete it.

Specifically avoid:

- **Interfaces with a single implementation.** That is not abstraction but an extra hop. Interfaces needed for DI and test mocking are fine; elsewhere, extract when a second implementation actually exists.
- **Extension points reserved for "it might change later"** — config switches, strategy patterns, generic parameters, event hooks. Change it when it really changes; that is usually cheaper than guessing wrong now.
- **Generalization beyond the requirement.** The requirement is to export orders: build order export, not a "generic export framework".
- **Extra layers.** Insert no intermediate layer that only forwards calls.

**Priorities**: security > readability > performance (AGENTS.md → General Principles). When they conflict, trade off in that order; a lower priority never sacrifices a higher one. Most code satisfies all three at once — never use this list to justify "hard to read for performance" unless you measured it and wrote the measurements into the plan.

Likewise, the Plan only realizes behavior the Spec defines. **Plan no feature the Spec does not contain**; if something seems missing, report it for the user to decide whether to add it to the Spec, instead of adding it in the Plan.

## Research before introducing a third-party package

If the plan seems to need a new dependency, **research it before writing it into the plan**; never write "install package XXX" in the task list as if it were decided.

The reason is simple: once a package is a dependency, its license binds the product and its vulnerabilities are ours. This is a legal and security matter, not just a technology choice.

### First question: is it really needed

Check in order and stop at the first level that works:

1. **Does the language / framework have it built in?** The standard library covers more than most people think.
2. **Do existing dependencies cover it?** Check the project's dependency lists (e.g. `Directory.Packages.props`, `package.json`); never add a second package of the same kind.
3. **Do a few dozen lines of our own code solve it?** Pulling in a whole dependency for a small helper costs more to maintain long-term than writing it.
4. Only if all are no, evaluate a package.

### What must be verified

The package rules (version, release age, maintenance, main-package constraint, license, transitive dependencies) are defined in AGENTS.md → Dependencies; apply them as written and run the `0-package-compliance` skill on the candidate. In addition:

**License (legal risk, top priority)**

- Find the exact license and write it into the plan. Never write "open source" — write `MIT`, `Apache-2.0`, `BSD-3-Clause`.
- **No license declared** means all rights reserved — **not usable**. No license does not mean free to use.
- **Commercial / dual license** — requires purchase and must be raised first.

**Security**

- Check known vulnerabilities: the package manager's audit command (e.g. `dotnet list package --vulnerable --include-transitive`, `pnpm audit`), plus GitHub Advisory / NVD.
- An unpatched high-risk CVE is a reason to reject.
- Watch for supply-chain signals: a name close to a well-known package (typosquatting), a publisher with no other work, a recent ownership change.

**Maintenance**

- Last update, open-issue count, support for the project's runtime version.
- A single-maintainer package is a risk to flag in the plan.
- Download counts are a hint, not a security guarantee.

**Sources**: the package's official registry page (NuGet / npm) and source repository are primary sources. **Say when you are unsure; never write license terms from memory** — a wrong license is more dangerous than none.

### Writing it into the plan

Fill in the 「第三方套件評估」 section, then **raise it at delivery and wait for the user to decide**. License and security are the user's decisions, not yours.

When only a GPL option exists, raise it anyway with alternatives (build it, change approach, or buy a commercial license) for the user to judge.

## Spec mapping

The 「Spec 對照」 table is the basis of the Verify stage and cannot be omitted.

An FR or acceptance scenario without a matching implementation is a gap in the plan; an implementation that maps to no Spec item is something nobody asked for — check whether the Spec missed it or the scope crept.

## Behavior the Spec does not define

Planning often hits details the Spec does not cover (how to respond to concurrent conflicts, what an empty list shows, how overlong fields are truncated).

**Do not pick one and carry on.** Stop and ask the user, with your recommendation and reasoning.

If the question concerns external behavior the Spec should define, say so in the report — the Spec may need an addition. Repeated questions the Plan cannot settle usually mean the Spec has too many gaps; go back and fill them in the Spec stage instead of forcing it in the Plan.

## Delivery

Write the plan to `docs/plans/`, then tell the user:

- The file location
- Which User Stories it covers and how many tasks
- Any deviation from existing architecture patterns, and why
- The technical trade-offs you made
- **Every third-party package to add, with its license and security findings**. Flag GPL/AGPL, missing licenses, required purchases or unpatched vulnerabilities explicitly for the user's ruling — never assume they are usable.

Then **stop and wait for review**. No implementation without approval.

The Plan is disposable and can be removed after implementation; it never goes into `docs/specs/` — that holds only feature specifications.

## During implementation

After approval, the Code stage begins. If implementation shows the plan cannot work (the existing structure does not support it, performance is infeasible), stop, explain, and propose an adjustment — never deviate silently from the approved plan.

After implementation, the Verify stage accepts the work against the Spec's acceptance scenarios and success criteria.
