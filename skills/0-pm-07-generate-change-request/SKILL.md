---
name: 0-pm-07-generate-change-request
description: Write a formal Change Request (CR, 變更請求單) for an out-of-scope requirement — gap analysis against the SOW/SRS, effort by role in man-days, schedule and risk impact, three options (approve / defer to Phase 2 / reject) and signature blocks — saved under docs/change-requests/. Use for any 🔴 item or scope change the client must sign (「開 CR」「變更請求單」「追加需求評估」「紅燈需求」).
---

# Write the CR

A CR puts a price on a scope change and gets it signed before any work starts. The client chooses; our job is to make the choice clear: what changes, what it costs in man-days and schedule, and what each option means.

> **Language**: written in Traditional Chinese (正體中文). Keep IDs in English.

## Steps

### 1. Load the inputs

- The request — from the conversation, a MOM 🔴 item, or a `0-pm-06-validate-requirement-scope` report
- The baseline documents and scope-defense rules in [`../0-pm-06-validate-requirement-scope/references/scope-defense.md`](../0-pm-06-validate-requirement-scope/references/scope-defense.md)
- Open risks in the latest file in `docs/risks/`
- The current milestones (SOW section 3) for the schedule impact

Number the CR `CR-{YYYYMMDD}-{NN}`: `NN` is one past the highest number already used on that date in `docs/change-requests/`, starting at 01.

### 2. Gap analysis

- **As-Is** — what the baseline says, quoted with its ID; or 「SOW 未涵蓋」
- **To-Be** — what the client now asks for, in their terms
- **Gap** — what has to be built or changed to get from one to the other

### 3. Impact analysis

- **Effort** by role (PM, SA/BA, 開發, QA) in man-days, with what each role does. Estimates come from the user or team when given; otherwise write your own breakdown and mark the total 「AI 初估，待 PM 確認」. In UAT or later, include re-test and regression effort.
- **Billing** — Billable (new scope, charged to the client) or Absorbed (clarification of existing scope, absorbed by the team). Unclear → `TODO: 計費歸屬 — PM`.
- **Schedule** — working days needed, original and new go-live dates, critical-path impact.
- **Risks** — technical and business risks of the change (architecture, regression, integration, user impact). For each open 🔴 or 🟡 risk in `docs/risks/` that this change worsens or touches, cite its ID with the reason; cite only IDs that exist in that file.

### 4. Options and recommendation

Always all three:

- **Option A — 核准執行**: added man-days, new go-live date; the signed CR becomes part of the scope baseline
- **Option B — 延後至 Phase 2**: no impact on this phase; refine with real usage after launch
- **Option C — 拒絕**: the reason (technical limits, risk, conflict with product direction) and any alternative

State the PM recommendation with its reason; usually B unless the change blocks launch.

### 5. Write

Write to `docs/change-requests/CR-{YYYYMMDD}-{NN}_{標題}.md` with status 草稿.

## After signing

With the user's consent, record the client's choice, signature date and status (已核准／已延後／已駁回) in the CR. An approved CR is baseline from then on: the requirement enters the next SRS version citing `[Ref: CR-…]`, and the SOW stays as signed.

## Template

```markdown
# 變更請求單 CR-[YYYYMMDD]-[NN]：[標題]

- **申請日期**：YYYY-MM-DD
- **提出人**：[客戶姓名／單位]
- **來源**：[docs/meetings/xxx.md Q[n]／範疇判定／口頭或郵件，附日期]
- **狀態**：草稿

## 1. 變更摘要

[一兩句說明客戶要什麼]

## 2. 落差分析

### 2.1 原始範圍（As-Is）

`[Ref: SOW-2.x.x]`「[原文]」（或：SOW 未涵蓋）

### 2.2 新需求（To-Be）

[客戶要求的內容]

### 2.3 差異

[需新增或修改的部分]

## 3. 影響評估

### 3.1 工作量

| 角色 | 工作項目 | 人天 |
| :--- | :--- | :--- |
| PM | [項目] | [n] |
| SA/BA | [項目] | [n] |
| 開發 | [項目] | [n] |
| QA | [項目] | [n] |
| **合計** | | **[n]**（[來源或「AI 初估，待 PM 確認」]） |

**計費歸屬**：Billable（新增範圍，向客戶追加）／Absorbed（範圍釐清，團隊吸收）

### 3.2 時程

- **所需工作天**：[n]
- **原定上線日**：[YYYY-MM-DD]
- **調整後上線日**：[YYYY-MM-DD]
- **要徑影響**：[說明]

### 3.3 風險

| 風險 | 說明 | 緩解措施 |
| :--- | :--- | :--- |
| [風險] | [說明] | [措施] |

**受影響的既有風險** *（有才寫）*：

| 風險 ID | 影響 | 說明 |
| :--- | :--- | :--- |
| [T-01] | 加劇／相關 | [原因與建議調整] |

## 4. 處理選項

### Option A：核准執行

- **追加**：[n] 人天
- **上線日**：延後至 [YYYY-MM-DD]
- **優點**：[…]　**缺點**：[…]

### Option B：延後至 Phase 2

- **本期影響**：無
- **優點**：[…]　**缺點**：[…]

### Option C：拒絕

- **理由**：[…]
- **替代方案**：[若有]

**PM 建議**：[選項與理由]

## 5. 簽核

**客戶決策**：

- [ ] Option A：核准執行（同意追加 [n] 人天及時程調整）
- [ ] Option B：延後至 Phase 2
- [ ] Option C：拒絕

| 角色 | 姓名 | 簽名 | 日期 |
| :--- | :--- | :--- | :--- |
| 客戶代表 | | | |
| 專案經理 | | | |
```

## Delivery

Tell the user:

- The file location and CR number
- Total man-days, billing, schedule impact and the recommendation — in one short block
- Whether the estimate is the team's or an AI first estimate awaiting PM confirmation
- Every existing risk it affects, and every `TODO:`

Until the client signs, the work stays frozen.
