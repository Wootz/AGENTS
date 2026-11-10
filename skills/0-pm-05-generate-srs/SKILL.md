---
name: 0-pm-05-generate-srs
description: Write the client-signed Software Requirements Specification (SRS, 需求規格書) from the SOW, the 🟢 MOM decisions and signed CRs — user stories, business rules, data constraints, binary acceptance criteria and NFRs, every requirement traced to a SOW clause or CR — saved under docs/srs/. Use once requirement meetings are settled (「寫 SRS」「需求規格書」「綠燈項目轉規格」「準備客戶簽核的規格」).
---

# Write the SRS

The SRS is the requirement document the client signs: it turns the agreed scope into requirements precise enough to accept or reject without argument. Two rules carry it:

- **Traceability**: every requirement cites its scope source — `[Ref: SOW-2.1.x]` or `[Ref: CR-YYYYMMDD-NN]` — and the MOM where it was agreed. A requirement with no source does not belong in the SRS.
- **Binary acceptance**: every criterion is Pass or Fail. 「效能良好」「介面友善」「操作直覺」 cannot be accepted or rejected; 「登入回應 < 3 秒」「3 次點擊內完成結帳」 can.

The SRS is client-facing and project-wide. `0-generate-spec` later splits it into per-feature, development-facing Specs.

> **Language**: written in Traditional Chinese (正體中文). Keep IDs (`REQ-LOGIN-001`, `SOW-2.1.1`) and data types in English.

## Steps

### 1. Collect the agreed scope

Read:

- The latest SOW in `docs/sow/` — required; none → ask the user for it
- Every MOM in `docs/meetings/` — take 🟢 decisions and 🟡 items ruled 🟢
- CRs in `docs/change-requests/` with status 已核准
- The current SRS in `docs/srs/`, when revising

What stays out: 🔴 items without a signed CR, and 🟡 items not yet ruled. List the unruled 🟡 items for the user; they block the requirements they touch.

**Done when** every in-scope SOW item is either covered by agreed MOM decisions or noted as 「尚未訪談」 for delivery.

### 2. Write the requirements

Group by module. Each requirement `REQ-{MODULE}-NNN` has:

- **User story** — 身為 [角色]，我希望 [具體動作]，以便 [價值]. Roles are defined once in section 1.2.
- **Source** — `[Ref: SOW-2.1.x]` or `[Ref: CR-…]`, plus the MOM file and Q number
- **Business rules** — validation, flow, constraints: 「連續登入失敗 3 次，帳號鎖定 15 分鐘」「密碼變更後，既有登入 Token 立即失效」
- **Data constraints** — for every noun the user story handles: type, length / range, format, required, uniqueness. 「電話：String(10)，必填，格式 `09\d{8}`，系統內唯一」, never just 「電話欄位」. Enough for a DBA to draft the schema.
- **Priority** — P0 (core business), P1 (important), P2 (enhancement)
- **Acceptance criteria** — binary, each one testable as written

A detail the MOMs leave open becomes `TODO: <the specific question> — <who decides>` under that requirement. Fill no gap with a guess: a guessed rule in a signed SRS becomes a contractual obligation.

Numbers, once issued, are never reordered or reused; a dropped requirement is marked 已廢止.

### 3. Write the NFRs

Take NFRs from the SOW and MOMs, citing the source. Where they are silent, use these defensive defaults, each tagged （預設）so the client sees them before signing:

| Area | Default |
| :--- | :--- |
| 效能 | 一般操作回應 < 3 秒（單一使用者、正式環境） |
| 相容性 | 最新版 Chrome、Edge 桌面版；不含 IE |
| 資安 | 全站 HTTPS；密碼以雜湊儲存 |
| 可用性 | 不承諾 SLA；服務時間依 SOW 維運條款 |

### 4. Self-check and write

- [ ] Every requirement cites a SOW clause or a signed CR, and its MOM
- [ ] No 🔴 item without a signed CR; no unruled 🟡 item
- [ ] Every acceptance criterion is binary; no subjective adjectives anywhere
- [ ] Every data field has type, length / range, format, required
- [ ] Every default NFR is tagged （預設）

Write to `docs/srs/SRS_{專案名稱}_v{版本}.md`.

## Versions

- **Draft**: edit in place.
- **Signed** (狀態 set to 已簽署 when the user confirms signature): frozen; never edit it. A revision is a new file with the version bumped and a 修訂紀錄 row naming the source (CR, MOM). Changed requirements keep their numbers.

## Template

```markdown
# [專案名稱] 軟體需求規格書（SRS）

- **版本**：v1.0
- **日期**：YYYY-MM-DD
- **狀態**：草稿
- **SOW 基準**：[docs/sow/SOW_xxx_v1.0.md]

## 修訂紀錄

| 版本 | 日期 | 變更 | 依據 |
| :--- | :--- | :--- | :--- |
| v1.0 | YYYY-MM-DD | 初版 | [MOM 清單] |

## 1. 專案概述

### 1.1 目標

[取自 SOW 第 1 節]

### 1.2 使用者角色

| 角色 | 說明 |
| :--- | :--- |
| [角色] | [職責與使用情境] |

## 2. 功能需求

### 2.1 [模組名稱]

#### REQ-[MODULE]-001：[需求標題]

- **使用者故事**：身為 [角色]，我希望 [動作]，以便 [價值]。
- **來源**：`[Ref: SOW-2.1.x]`；[docs/meetings/YYYY-MM-DD_主題.md] Q[n]
- **優先級**：P0

**業務規則**：

1. [規則]

**資料限制**：

| 欄位 | 型別 | 長度／範圍 | 必填 | 格式與限制 |
| :--- | :--- | :--- | :--- | :--- |
| [欄位] | String | 10 | ✓ | [格式、唯一性] |

**驗收標準**：

- [ ] [可判定 Pass／Fail 的條件]

## 3. 非功能需求

### 3.1 效能
### 3.2 資安
### 3.3 相容性
### 3.4 可用性

[每項皆為可量測數值，並註明來源：SOW 條款、MOM，或標示（預設）]

## 4. 簽核

| 角色 | 姓名 | 簽名 | 日期 |
| :--- | :--- | :--- | :--- |
| 客戶代表 | | | |
| 專案經理 | | | |
```

## Delivery

Tell the user:

- The file location and the requirement count per module and priority
- **Every `TODO:`, listed for answering** — the SRS is not ready for signature until they are cleared
- SOW items not yet covered (尚未訪談) and unruled 🟡 items left out
- Every （預設） NFR, so they can be confirmed before the client sees them

After the client signs, `0-generate-spec` turns each feature into a Spec.
