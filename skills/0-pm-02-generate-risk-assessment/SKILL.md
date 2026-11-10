---
name: 0-pm-02-generate-risk-assessment
description: Write a project risk assessment (風險評估) — risks across technical, requirement, schedule and operational categories, scored probability × impact, each with mitigation, contingency, owner and monitoring trigger — saved under docs/risks/. Use at project kickoff or before bidding, and to reassess later (「風險評估」「風險清單」「盤點專案風險」「重新評估風險」).
---

# Write the risk assessment

A risk assessment lists what could derail the project, how bad each is, and what is done about it. A risk is only useful when someone owns it and a trigger says when to act — a list of worries with no owner is noise.

> **Language**: written in Traditional Chinese (正體中文). Keep risk IDs (`T-01`) in English.

## Steps

### 1. Gather context

Read whichever exist: the SOW in `docs/sow/`, MOMs in `docs/meetings/`, the previous assessment in `docs/risks/`, and what the user gives — goals, constraints, stakeholders, technical environment, schedule pressure.

**Reassessment**: start from the latest file in `docs/risks/`. Continuing risks keep their IDs; re-score each; mark closed ones 已關閉 with the reason; new risks take the next free ID in their category. IDs are never reused.

**Done when** each category below has been considered against this project's facts.

### 2. Identify risks by category

| Category | ID | Look for |
| :--- | :--- | :--- |
| 技術 | `T-` | Legacy integration, third-party API reliability, performance targets, security / regulation (個資法、PCI-DSS), data migration integrity |
| 需求與範疇 | `R-` | Ambiguous acceptance criteria, vague SOW items, scope-creep pressure, complex business rules, conflicting departments |
| 時程與資源 | `S-` | No buffer, vendor / third-party delays, test-resource availability, slow client review |
| 組織與營運 | `O-` | Unclear decision authority, stakeholder conflict, user resistance, missing training, undefined escalation |

Each risk is a concrete fact about this project, with its source: 「SOW-2.3.2 金流 API 規格需客戶提供，未訂期限」, not 「第三方可能延遲」. A category with nothing found is written as 本次未識別.

### 3. Score severity

Severity = probability × impact:

- **Probability** — 高 > 70%, 中 30–70%, 低 < 30%
- **Impact** — 高: launch-blocking or > 2 weeks of delay; 中: workaround needed or 1–2 weeks of delay; 低: minor

| Probability \ Impact | 高 | 中 | 低 |
| :--- | :--- | :--- | :--- |
| **高** | 🔴 高 | 🔴 高 | 🟡 中 |
| **中** | 🔴 高 | 🟡 中 | 🟢 低 |
| **低** | 🟡 中 | 🟢 低 | 🟢 低 |

### 4. Plan the response

Every 🔴 and 🟡 risk gets all four; 🟢 risks get mitigation only:

- **Mitigation** — concrete steps that lower the probability or the impact
- **Contingency** — what to do once it happens
- **Owner** — a role or name; unknown → `TODO: 指派負責人 — PM`
- **Monitoring** — the indicator, how often it is checked, and the trigger that escalates it

**Done when** every 🔴 and 🟡 risk has all four.

### 5. Write

Write to `docs/risks/風險評估_{YYYY-MM-DD}.md`. Each assessment is a dated snapshot; earlier files stay as they are.

## Template

```markdown
# [專案名稱] 專案風險評估

- **評估日期**：YYYY-MM-DD
- **評估人**：[姓名]
- **專案階段**：[簽約前／需求／開發／測試]
- **前次評估**：[docs/risks/風險評估_YYYY-MM-DD.md，首次評估則刪除此行]

## 1. 摘要

| 嚴重度 | 數量 | 項目 |
| :--- | :--- | :--- |
| 🔴 高 | [n] | [T-01、R-02] |
| 🟡 中 | [n] | [ID] |
| 🟢 低 | [n] | [ID] |

## 2. 風險分析

### 2.1 技術風險

| ID | 風險 | 來源 | 機率／衝擊 | 嚴重度 | 緩解措施 | 應變計畫 | 負責人 | 狀態 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| T-01 | [具體事實] | [SOW-2.3.2／MOM／訪談] | 高／高 | 🔴 高 | 1. [步驟] | [發生後怎麼做] | [角色] | 追蹤中 |

### 2.2 需求與範疇風險

[同上表格，ID 為 R-]

### 2.3 時程與資源風險

[同上表格，ID 為 S-]

### 2.4 組織與營運風險

[同上表格，ID 為 O-]

## 3. 監控計畫

| ID | 監控指標 | 頻率 | 觸發條件 | 升級對象 |
| :--- | :--- | :--- | :--- | :--- |
| T-01 | [指標] | 每週 | [達到什麼狀況就升級] | [角色] |

## 4. 立即行動

- [ ] [行動] — [負責人]，[期限]

## 5. 已關閉風險 *（重新評估時才寫）*

| ID | 風險 | 關閉原因 |
| :--- | :--- | :--- |
```

## Delivery

Tell the user:

- The file location and the count per severity
- Every 🔴 risk in one line each, with its immediate action
- Every `TODO:` (owners, missing facts)
- On reassessment: what changed severity, what closed, what is new
