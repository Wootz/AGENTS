---
name: 0-pm-06-validate-requirement-scope
description: Classify a new or changed requirement against the agreed scope (SOW, SRS, approved CRs) with the traffic light (🟢🟡🔴), citing the exact clause, and return a ready-to-send client reply. Use whenever a client raises something new at any stage, or says 「順便」「應該要有」「也可以」「改一下」「這很快吧」 (「這在範圍內嗎」「範疇檢查」「客戶臨時加需求」).
---

# Validate requirement scope

A quick scope check for one request, before anyone says yes. It answers three things: is this in scope, which clause says so, and what do we tell the client.

> **Language**: the report and client reply are in Traditional Chinese (正體中文). Keep clause IDs in English.

## Steps

### 1. Load the rules and the baseline

Read [`references/scope-defense.md`](references/scope-defense.md) — the traffic light, warning keywords and scripts all live there.

Load the baseline documents it defines; no SOW at all → ask the user for it. Ask for the project phase (簽約前／需求／開發／測試) when the conversation does not show it; it changes the recommended action.

### 2. Search for evidence

Search the baseline for the request's feature, module and keywords: SOW 2.1 and 2.2, SRS requirements, approved CRs. Quote every relevant clause with its ID.

**Done when** each related clause is quoted, or the search found none — say which terms were searched.

### 3. Classify and respond

Apply the traffic light from `scope-defense.md`. Then:

- 🟢 — the clause, and where it goes next: requirements phase → MOM and SRS; development or later → already in the SRS (cite the REQ) or an SRS gap to report
- 🟡 — the two readings, the PM who rules, the ruling date
- 🔴 — the reason, an effort estimate by role (from the user when given; otherwise your own marked 「AI 初估，待 PM 確認」) and schedule impact. In UAT or later, include the re-test effort.

Pick the script per light and per warning keyword, with every placeholder filled.

## Report format

Reply in the conversation:

```markdown
## 範疇判定：[需求摘要]

**結果**：🟢 範圍內／🟡 待 PM 判定／🔴 超出範圍
**依據**：[條文 ID]「[原文]」（或：已搜尋 [關鍵字]，基準文件無相關條文）
**警示用語**：[原句，若有]

**建議行動**：[依燈號與專案階段]

**影響評估** *（🔴 才寫）*：
| 角色 | 人天 |
| :--- | :--- |
| 合計 | [n]（[來源或「AI 初估，待 PM 確認」]） |
時程影響：[n] 天

**回覆客戶話術**：
> [已填好的話術]
```

Write it to `docs/scope-validations/{YYYY-MM-DD}_{功能名稱}.md` only when the user asks for a file.

## Delivery

After the report: 🔴 → offer `0-pm-07-generate-change-request`; requirements phase → the decision belongs in the next MOM (`0-pm-04-generate-mom`).
