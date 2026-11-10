---
name: 0-pm-04-generate-mom
description: Turn a meeting transcript or notes into minutes of meeting (MOM, 會議紀錄) with a traffic-light scope call (🟢🟡🔴) on every decision against the SOW, plus action items and confirmed exclusions — saved under docs/meetings/. Use after a requirement meeting or when a transcript is pasted (「整理會議紀錄」「產生 MOM」「貼上逐字稿」「訪談後紀錄」).
---

# Write the MOM

The MOM is the written record of what was agreed and the gate between conversation and specification: only 🟢 decisions flow into the SRS. Its value is defensive — half a year later it must show who agreed to what, and that out-of-scope requests were flagged when they were made.

> **Language**: written in Traditional Chinese (正體中文). Keep clause and item IDs in English.

## Steps

### 1. Load the baseline and the outline

Read [`../0-pm-06-validate-requirement-scope/references/scope-defense.md`](../0-pm-06-validate-requirement-scope/references/scope-defense.md) now; its traffic-light rules decide step 3.

Load the baseline documents it defines. No SOW at all → ask the user for it. If they confirm none exists, every decision is 🟡 and the MOM states 「無 SOW 基準」 at the top.

If an interview outline for this meeting exists in `docs/interviews/`, read it: questions it planned that the transcript never answers go into 待追問.

### 2. Extract Q&A and decisions

From the transcript:

- Attendees, split into our side and the client
- Every question that clarifies a requirement, with its conclusion. Keep only what bears on scope or specification.
- A conclusion is a decision only when someone agreed to it (「確認」「同意」「就這樣」). Discussed but unsettled → 未決, with who must decide.

Record what was said. When the transcript leaves the speaker or the conclusion unclear, write `TODO: <what is unclear> — 與會者確認`.

Flag every warning keyword (`scope-defense.md` → Warning keywords) where it occurs.

### 3. Classify every request

Apply the traffic light to every requirement raised — agreed or still 未決 — and record what `scope-defense.md` requires for that light. Effort estimates live in the CR, not the MOM.

**Done when** every Q has a light and every 🟢 cites a clause.

### 4. Action items and exclusions

- **Action items**: who does what by when; split client and internal. A missing owner or due date is a `TODO:`.
- **Exclusions**: everything the client said they do not need or we declined — the meeting's own record of 「不做」.

### 5. Write

Write to `docs/meetings/{YYYY-MM-DD}_{主題}.md`.

### 6. Ask which 🔴 items get a CR

Many 🔴 items end as Phase 2 or are dropped, so list them at delivery and ask which need a CR. For each one chosen, run `0-pm-07-generate-change-request` and put its number in the MOM; several 🔴 items that together make one feature go in one CR. The rest record the client's choice (Phase 2, dropped) in the CR column.

## Template

Under each Q, write only the block for the light it got.

```markdown
# 會議紀錄：[主題]

- **會議時間**：YYYY-MM-DD HH:MM
- **地點／連結**：[地點]
- **出席（我方）**：[姓名]
- **出席（客戶）**：[姓名]
- **SOW 基準**：[docs/sow/SOW_xxx_v1.0.md]
- **訪談大綱**：[docs/interviews/xxx.md，沒有則刪除此行]

## 1. 問答與決議

### Q1：[問題摘要]

[問題背景，一兩句]

**結論**：[結論]

🟢 **範圍內** — `[Ref: SOW-2.1.x]`「[條文原文]」→ 納入 SRS

🟡 **待 PM 判定** — 依據 SOW-2.1.x「[條文原文]」
- 解讀 A：[…]
- 解讀 B：[…]
- 判定人：[PM]，期限：[YYYY-MM-DD]

🔴 **超出範圍** — [SOW-2.2.x 明確排除／SOW 未涵蓋]
- CR：[CR 編號／待決定／Phase 2／不做]
- ⚠️ 客戶簽署 CR 前不得開發

⚠️ **警示用語**：[「順便…」原句，若有]

## 2. 未決事項 *（有才寫）*

| 事項 | 需誰決定 | 期限 |
| :--- | :--- | :--- |

## 3. 待辦事項

| ID | 事項 | 負責方 | 負責人 | 期限 |
| :--- | :--- | :--- | :--- | :--- |
| AI-01 | [事項] | 客戶／我方 | [姓名] | [YYYY-MM-DD] |

## 4. 確認不做的事項

- [項目]（[客戶表示不需要／我方婉拒]）

## 5. 範疇彙整

### 🔴 超出範圍（需 CR）

| 項目 | 依據 | 客戶要求 | CR | 狀態 |
| :--- | :--- | :--- | :--- | :--- |
| Q[n] | [SOW-2.2.x／未涵蓋] | [要求] | [CR 編號／待決定] | [待決定／待簽核／Phase 2／不做] |

### 🟡 待 PM 判定

| 項目 | 依據條文 | 解讀 | 判定人 | 期限 | 判定結果 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| Q[n] | [SOW-2.1.x] | A／B | [PM] | [YYYY-MM-DD] | 待判定 |

### 🟢 範圍內（納入 SRS）

- Q[n] [項目] `[Ref: SOW-2.1.x]`

## 6. 待追問 *（有訪談大綱且有未回答問題才寫）*

- [大綱中未獲回答的問題]
```

## Recording a 🟡 ruling

When the PM rules on a 🟡 item later, update this MOM with the user's consent: fill 判定結果 with the ruling, the date and where it was recorded (email, next MOM). 🟢 → it flows to the SRS; 🔴 → draft a CR.

## Delivery

Tell the user:

- The file location and the count per light
- Every 🔴 item, asking which need a CR; every 🟡 item with its ruling deadline
- Every warning keyword found, with the suggested script
- Every `TODO:` and every 待追問 question

Next, once the topic's 🟡 items are ruled: `0-pm-05-generate-srs`.
