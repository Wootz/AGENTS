---
name: 0-pm-01-generate-sow
description: Write a Statement of Work (SOW, 工作說明書) that locks the contract scope — numbered in-scope items, explicit exclusions, dependencies, client responsibilities, milestones — saved under docs/sow/. Use before bidding or signing, or whenever the scope boundary must be defined (「寫 SOW」「工作說明書」「定義專案範圍」「排除條款」「簽約前鎖範圍」).
---

# Write the SOW

The SOW is the scope baseline every later document cites: interview outlines, MOM traffic lights, the SRS and CRs all point at its clause IDs. Two things make it work:

- **Every item has a stable ID** (`SOW-2.1.3`), so later documents can cite it.
- **The exclusions name what the client is most likely to assume is included**, so the gray areas are settled before signing instead of during UAT.

> **Language**: the SOW is written in Traditional Chinese (正體中文). Keep clause IDs in English.

## Steps

### 1. Gather inputs

From the conversation and any files the user gives: background and goals, requested features, constraints (platforms, dates, team), known dependencies.

Facts with contractual effect — delivery items, dates, acceptance period, review turnaround — come only from the user. Each one missing becomes `TODO: <the specific question> — <who decides>` in the document.

**Done when** every template section has content or a `TODO:`.

### 2. Write the in-scope items

Group by module or deliverable. Each item is one concrete sentence stating what is delivered and where its boundary is:

| Vague (becomes a 🟡 in every meeting) | Concrete |
| :--- | :--- |
| 會員模組 | 會員註冊、帳號密碼登入、忘記密碼（Email 重設連結） |
| 報表功能 | 月銷售報表線上查詢（不含匯出） |

The boundary is contractual too: when the user's input names a feature without its boundary, write what was given and add a `TODO:` asking for the boundary.

Number them `SOW-2.1.1`, `SOW-2.1.2`, … in order.

### 3. Write the exclusions

The most important section. Rule: **whatever is not explicitly paid for and could plausibly be assumed included gets excluded explicitly.** Walk the checklist below and keep only the items plausible for this project — exclusions nobody would assume dilute the list.

Where it applies, state what is provided instead: 「不支援行動裝置 RWD（僅支援桌面版 1920×1080）」.

| Category | Commonly assumed included |
| :--- | :--- |
| 技術層面 | 多語系、行動裝置 RWD、舊版瀏覽器（IE 11）、Email／簡訊通知、報表匯出（Excel／PDF） |
| 品質與維運 | 高可用性（SLA > 99.5%）、7×24 值班、災難復原、壓力測試（> 100 TPS）、保固期後維護 |
| 資料 | 歷史資料清洗與轉換、舊系統資料遷移 |
| 環境與第三方 | 主機與雲端費用、SSL 憑證、第三方服務授權與串接（金流、簡訊、地圖） |
| 智慧財產 | 原始碼所有權移轉 |

Number them `SOW-2.2.1`, … in order.

### 4. Write dependencies and client responsibilities

- **Dependencies (2.3)**: everything that blocks the team, each with who provides it and by when — test environment / VPN accounts, API specs and keys, client data, hardware.
- **Client responsibilities (2.4)**: always include the day-for-day slip clause (client delay shifts the schedule by the same number of days) and a review turnaround for documents sent for sign-off.

### 5. Write milestones and acceptance

Phases (design → development → UAT → go-live), each with deliverables and target date. Acceptance is binary: SRS criteria judged Pass/Fail item by item, a fixed UAT period, no open P0/P1 defects.

### 6. Self-check and write

- [ ] Every in-scope item is concrete enough that a 🟢/🔴 call can cite it
- [ ] Every exclusion is something this client could plausibly assume
- [ ] No subjective wording (「友善」「快速」「完整」); every target is measurable
- [ ] Every missing contractual fact is a `TODO:`

Write to `docs/sow/SOW_{專案名稱}_v{版本}.md`.

## Versions

- **Draft**: edit in place.
- **Signed** (狀態 set to 已簽署 when the user confirms signature): frozen; never edit it. Scope changes go through `0-pm-07-generate-change-request`. Consolidate approved CRs into a new SOW version (new file, version bumped, a 修訂紀錄 row citing the CRs) only when the user asks.

## Template

```markdown
# [專案名稱] 工作說明書（Statement of Work）

- **版本**：v1.0
- **日期**：YYYY-MM-DD
- **狀態**：草稿
- **撰寫人**：[姓名]

## 修訂紀錄

| 版本 | 日期 | 變更 | 依據 |
| :--- | :--- | :--- | :--- |
| v1.0 | YYYY-MM-DD | 初版 | — |

## 1. 專案背景與目標

[要解決的核心問題與預期成果]

## 2. 專案範疇

### 2.1 交付項目（In-Scope）

#### [模組或交付物類別]

- **SOW-2.1.1** [具體交付內容與邊界]

### 2.2 排除項目（Out-of-Scope）

#### [技術層面／品質與維運／資料／環境與第三方／智慧財產]

- **SOW-2.2.1** 不包含 [項目]（[實際提供的範圍，若有]）

### 2.3 假設與依賴

- **SOW-2.3.1** [依賴項目]，由 [客戶／第三方] 於 [YYYY-MM-DD] 前提供

### 2.4 客戶配合事項

- **SOW-2.4.1** [配合事項與期限]
- **SOW-2.4.2** 因客戶延遲提供 2.3、2.4 所列項目，專案時程依延遲天數順延。
- **SOW-2.4.3** 各階段文件送審後，客戶於 [N] 個工作天內回覆審閱意見。

## 3. 交付時程與里程碑

| 階段 | 里程碑 | 交付物 | 預計日期 |
| :--- | :--- | :--- | :--- |
| Phase 1 | [需求確認] | [SRS] | [YYYY-MM-DD] |

## 4. 驗收標準

- 功能依已簽署 SRS 之驗收標準逐條判定 Pass／Fail。
- UAT 期間為 [N] 個工作天；期滿無未結之 P0／P1 缺失即視為驗收通過。
- 範疇變更依變更請求（CR）流程辦理，經雙方簽署後生效。

## 5. 簽核

| 角色 | 姓名 | 簽名 | 日期 |
| :--- | :--- | :--- | :--- |
| 客戶代表 | | | |
| 專案經理 | | | |
```

## Delivery

Tell the user:

- The file location, and how many in-scope items and exclusions it has
- **Every `TODO:`, listed for answering**
- Every exclusion you added from the checklist rather than the user's input — each needs the user's confirmation before the SOW goes to the client

Next: `0-pm-02-generate-risk-assessment`, then `0-pm-03-generate-interview-outline` for each requirement meeting.
