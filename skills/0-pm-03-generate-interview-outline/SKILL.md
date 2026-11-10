---
name: 0-pm-03-generate-interview-outline
description: Prepare a requirement interview outline (需求訪談大綱) anchored on the SOW — relevant in-scope and excluded clauses, open-ended deep-dive questions, edge cases and risks per discussion item, and scope-defense scripts ready for the meeting — saved under docs/interviews/. Use before a requirement clarification meeting (「準備需求訪談」「訪談大綱」「訪談問題」「需求釐清會議」).
---

# Prepare the interview outline

An interview outline lets the interviewer dig out hidden complexity while holding the SOW line in the room. It does two jobs:

- **Uncover**: open-ended questions and edge cases, so the answers are detailed enough to write an SRS from.
- **Defend**: the relevant SOW clauses and ready-made scripts on the page, so a request outside scope gets a polite, consistent answer on the spot instead of an improvised yes.

> **Language**: written in Traditional Chinese (正體中文). Keep clause IDs in English.

## Steps

### 1. Load the scope baseline

Read [`../0-pm-06-validate-requirement-scope/references/scope-defense.md`](../0-pm-06-validate-requirement-scope/references/scope-defense.md) now; it defines the baseline, and step 3's scripts come from it.

Load the baseline documents it defines. No SOW at all → ask the user for it. If they confirm none exists, continue and state 「本次訪談無 SOW 基準」 at the top of the outline; every scope call in the meeting is then 🟡.

Also read earlier MOMs in `docs/meetings/` on the same topic: their open 🟡 items and 待追問 questions go into this outline.

**Done when** the in-scope and excluded clauses relevant to the meeting topic are listed with their IDs.

### 2. Design questions per discussion item

For each item the user wants to discuss:

- **Objective** — what must be known after the meeting to write the SRS for it
- **Baseline** — the SOW clause it falls under, or 「SOW 未涵蓋」 (a warning sign before the meeting starts)
- **Key questions** — open-ended (how / what / please describe), each one whose answer changes the specification:
  - ✅ 「貴公司 SSO 使用哪一套 Identity Provider？屬性對應有特殊要求嗎？」
  - ❌ 「需要 SSO 嗎？」
- **Edge cases** — failure, permission, data volume, concurrency, timeout, reversal (「退貨後等級要回溯嗎？」)
- **Boundary to confirm** — what is explicitly not needed (「有哪些瀏覽器或裝置確定不需要支援？」)
- **Risks** — tagged `[技術]` `[商業]` `[時程]`

**Done when** every discussion item has all six parts and every question passes the "answer changes the spec" test.

### 3. Pick the scripts

From `scope-defense.md`, copy the scripts this meeting is likely to need into the outline, with the topic's clause IDs and feature names already filled in — the interviewer reads them live and has no time to fill placeholders. At minimum: 🔴 Option B for each nearby exclusion, and 🟡 for each vague in-scope item.

### 4. Write

Write to `docs/interviews/{YYYY-MM-DD}_{主題}.md` — the meeting date, or today's when the meeting date is not set yet.

## Template

```markdown
# 需求訪談大綱：[主題]

- **會議日期**：YYYY-MM-DD HH:MM
- **預計時長**：[分鐘]
- **預計出席**：[我方]／[客戶]
- **SOW 基準**：[docs/sow/SOW_xxx_v1.0.md]

## 0. 範疇基準

### 本次相關的交付項目

- **SOW-2.1.x** [原文]

### 本次相關的排除項目（被提及時使用下方話術）

- **SOW-2.2.x** [原文]

### 前次未結事項 *（有才寫）*

- [🟡 待判定項目或待追問問題，附來源 MOM]

## 1. 討論項目

### 1.1 [項目名稱]

**訪談目標**：[會後必須知道什麼]
**範疇基準**：[SOW-2.1.x／SOW 未涵蓋]

**關鍵提問**：

1. [開放式問題]

**邊界情況**：

- [情境]

**需確認的不做範圍**：

- [問題]

**潛在風險**：

- **[技術]** [風險]

---

## 2. 現場應對話術

**[情境，例：客戶要求匯出 Excel（SOW-2.2.4）]**

> [已填好的話術]

## 3. 會後行動

- [ ] 24 小時內以 `0-pm-04-generate-mom` 產出會議紀錄並完成紅綠燈判定
- [ ] 決定開 CR 的 🔴 項目以 `0-pm-07-generate-change-request` 開立
- [ ] 🟡 項目於 2 個工作天內由 PM 判定
```

## Delivery

Tell the user the file location, the discussion items covered, and any item with no SOW coverage — those are the likeliest 🔴 of the meeting.
