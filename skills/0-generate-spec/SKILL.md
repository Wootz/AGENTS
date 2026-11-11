---
name: 0-generate-spec
description: Produce a Spec Kit–format feature specification (User Stories + Acceptance Scenarios + FR functional requirements + Success Criteria) from an SRS, requirement interview notes or designs, saved under docs/specs/ (「產出 Spec」「寫功能規格」「依 SRS 寫 spec」). Stage two of the SRS → Spec → Plan → Code → Verify flow. Writes only WHAT and WHY, never HOW; unclear requirements are marked [NEEDS CLARIFICATION] instead of guessed.
---

# Produce a Spec (Feature Specification)

A Spec defines **what users need and why**, written so it can serve directly as the acceptance basis. It is the only input to the Plan and Tasks.

It uses the **GitHub Spec Kit** specification format.

> **Language**: the Spec is written in Traditional Chinese (正體中文), as in the templates below. Keep these in English: RFC 2119 keywords (MUST / SHALL / SHOULD / MAY), Given / When / Then, the `FR-` `SC-` `US` prefixes, the `[NEEDS CLARIFICATION]` marker, and technical identifiers (field names, status values).

## Core principle

```
✅ Focus on what users need (WHAT) and why (WHY)
❌ Avoid how to build it (HOW) — no technology choices, API design, or code structure
```

This boundary keeps the Spec valid when the implementation technology changes. The test: **anything the implementation can change while externally observable behavior stays the same does not belong in the Spec.**

**Exception**: external API routes, request / response fields and error codes are an external contract that the frontend and tests depend on; they may go in the Spec.

## Never guess: use [NEEDS CLARIFICATION]

This is Spec Kit's most important mechanism and the part of this skill most easily skipped.

When a requirement is unclear, **do not pick a plausible interpretation and carry on**. LLMs naturally fill gaps — reading "login" as username/password login — but a wrong guess puts the whole Spec, and the Plan and implementation after it, on a false premise.

Write the Spec completely anyway, leaving a marker at each uncertainty:

```markdown
- **FR-006**: 系統 MUST 於案例送審後產生案例編號 [NEEDS CLARIFICATION: 退回重送時是否沿用原編號？]
- **FR-007**: 系統 MUST 保留已下架案例 [NEEDS CLARIFICATION: 保留期限未定義]
```

The marker format is `[NEEDS CLARIFICATION: <the specific question>]` — write **the question itself**, not just "to be confirmed", so the user can answer it directly.

This beats "no file until the requirements are confirmed": the skeleton takes shape and the uncertainties surface in one place instead of everything stalling. In exchange, **a Spec submitted for review with markers still in it does not pass** — see the checklist at the end.

## Review the SRS: raise risks and concerns

The SRS is whatever the user provides. When the project has one under `docs/srs/` (written by `0-pm-05-generate-srs`), use the latest version and cite its `REQ-` IDs in the Spec's 來源; a project without it is normal and needs nothing extra.

The SRS is the SA's deliverable, but **"delivered" does not mean "problem-free"**. Writing the Spec is the first time the requirements are examined line by line, and you will see things the SA may not have seen while writing. When you see them, say so.

This is different from `[NEEDS CLARIFICATION]`:

| | `[NEEDS CLARIFICATION]` | Risks and concerns |
| :--- | :--- | :--- |
| Targets | Gaps the requirements **do not cover** | Requirements that **are written but may be wrong** |
| Example | 「保留期限未定義」 | 「保留期限設 10 年，但個資法要求目的達成後應刪除」 |
| Where | Inline after the FR | Collected in the 「風險與疑慮」 section |
| Blocks delivery? | Yes — no Plan until markers are cleared | No, but must be raised explicitly at delivery |

### What to check actively

For each requirement in the SRS, ask:

- **Internal contradiction** — does this conflict with an earlier item? Does one concept have two definitions in different sections?
- **Conflict with the current state** — does it contradict existing behavior or data known from the conversation? Judge only from facts you know; make no further assumptions.
- **Regulation and personal data** — for personal data, retention periods, audit trails, cross-border transfer: does the approach hold up?
- **Security** — are permission boundaries clear? Who can see what? Is there an unrestricted operation like "anyone can download all data"?
- **Feasibility and scale** — can the current architecture handle the implied data volume, concurrency and response time? E.g. will "download all cases and attachments in one click" overload the server as data grows?
- **External dependencies** — does the external system really support this? When unconfirmable, mark `[NEEDS CLARIFICATION]`; never assume.
- **Missing negative paths** — does the requirement cover only the success path? What about failure, timeout, concurrent conflicts, insufficient permission?

### Format

Add a section at the end of the Spec (omit the whole section when there are no risks — never write 「無」 as filler):

```markdown
## 風險與疑慮

- **[風險] 一鍵下載無筆數上限**
  SRS 3.2 要求匯出全部案例與附件，但未設上限。以目前資料量估算單次匯出可能超過數 GB，
  會長時間佔用連線與磁碟。
  **建議**：設定筆數門檻，超過改為非同步產製後通知下載。
  **需要決定**：門檻訂在多少？

- **[矛盾] 草稿可見範圍前後不一致**
  SRS 2.1 寫「草稿僅建立者可見」，4.3 的畫面說明卻列出管理者可瀏覽所有草稿。
  **建議**：以 4.3 為準（管理者可見），因為它與現行實作一致。
  **需要決定**：請 SA 確認。
```

Each entry has four parts: a **type tag** (`[風險]` / `[矛盾]` / `[法規]` / `[資安]` / `[可行性]`), **concrete facts** (cite the SRS section; nothing vague), a **suggested approach**, and **who must decide what**.

### Judgment

- **Raise, never rewrite the requirement.** Your role is to surface the problem for someone to decide, not to rewrite the SA's requirements.
- **Be concrete.** "May have performance issues" is worthless; "a single export is estimated to exceed several GB" lets people judge.
- **Say when you are unsure.** If you are not certain it is a real problem, raise it anyway with your confidence level rather than swallowing it for fear of being wrong.
- **Never pad.** No risks, no section. Invented concerns dilute the few that matter.

## Document structure

Include these sections in order. Those marked *(必要)* cannot be omitted.

```markdown
# 功能規格：[功能名稱]

**建立日期**：[YYYY-MM-DD]
**狀態**：Draft
**來源**：[SRS 版本 / 工作項目編號 / 設計稿節點]

## 修訂紀錄

| 日期 | 來源 | 變更 |
| :--- | :--- | :--- |
| [YYYY-MM-DD] | [SRS v1.0] | 初版 |

## 使用者情境與測試 *(必要)*

### User Story 1 - [簡短標題] (Priority: P1)

[用平白的話描述這段使用者旅程]

**為何是此優先序**：[說明價值，以及為什麼排在這個位置]

**獨立測試方式**：[如何單獨驗證這個故事——例如「可透過建立一筆草稿案例並以一般使用者身分查詢來完整驗證」]

**驗收情境**：

1. **Given** [初始狀態]，**When** [動作]，**Then** [預期結果]
2. **Given** [初始狀態]，**When** [動作]，**Then** [預期結果]

---

### User Story 2 - [簡短標題] (Priority: P2)

[同上結構]

---

### 邊界情況

- 當 [邊界條件] 時會發生什麼？
- 系統如何處理 [錯誤情境]？

## 需求 *(必要)*

### 功能需求

- **FR-001**: 系統 MUST [具體能力]
- **FR-002**: 使用者 MUST be able to [關鍵操作]

### 主要實體 *(涉及資料時才寫)*

- **[實體名]**: [代表什麼、關鍵屬性，不寫實作型別]

## 成功標準 *(必要)*

### 可量測結果

- **SC-001**: [可量測的指標]
- **SC-002**: [可量測的指標]

## 假設

- [當需求未明確指定時，你所採用的合理預設]

## 風險與疑慮 *(有才寫)*

- **[類型] 標題**
  [具體事實，指出 SRS 哪一節]
  **建議**：[你的建議作法]
  **需要決定**：[需要誰決定什麼]
```

## Writing each section

### Every User Story must be independently testable

This is the key difference between a Spec and an SRS.

An SRS User Story usually stops at "As an admin, I want to add cases so that the knowledge base grows". A Spec User Story goes two steps further: **an independent test method** and **acceptance scenarios**.

Requiring each story to be independently testable lets it be developed, tested and delivered on its own — implementing only P1 should still yield a usable minimum viable version. The constraint forces stories to be cut in the right place: too fine and they cannot be delivered independently; too coarse and they cannot be tested completely.

Priorities P1 / P2 / P3 rank by value; P1 is the most critical.

### Functional requirements must be testable

Number every FR as `FR-XXX` so the Plan and Tasks can trace back. Once issued, numbers are never reordered; a dropped requirement is marked obsolete rather than having its number reused — otherwise cross-document tracing breaks.

A capability-style sentence (「系統提供查詢功能」) cannot be verified because it does not say what the result should be. Write it as behavior:

| Not verifiable | Verifiable |
| :--- | :--- |
| 系統提供草稿功能 | 系統 MUST 將草稿狀態的案例排除於一般使用者的查詢結果之外 |
| 支援樂觀鎖 | 更新請求挾帶的 rowVersion 與現況不符時，系統 MUST 拒絕更新並回報衝突 |
| 案例可分類 | 每筆案例 MUST 關聯至恰好一個樣態；未指定時系統 MUST 拒絕建立 |

RFC 2119 strength: **MUST / SHALL** are absolute — not meeting them is a defect; **SHOULD** is recommended, allowing justified exceptions; **MAY** is optional. Most business requirements use MUST.

### Success criteria must be measurable and technology-free

`SC-XXX` describes what counts as success, without technical terms — someone who does not know the implementation must be able to accept it.

| Fails | Passes |
| :--- | :--- |
| 查詢效能good | 一般使用者查詢案例列表，95% 的請求在 2 秒內回應 |
| 使用 Redis 快取避免重複下載 | 相同條件的重複匯出請求，5 分鐘內不重複產製檔案 |
| 索引同步正常 | 案例發布後 1 分鐘內可透過全文檢索找到 |

Note the second row: "use Redis" is HOW and belongs in the Plan; "no regeneration within 5 minutes" is measurable external behavior and belongs in the Spec.

### Key entities: concepts only

Only features involving data need this section. Write what each entity represents, its key attributes and relationships — but **no types, tables or key strategies**; those are the Plan's data model.

### Edge cases and assumptions

Edge cases cover business branches with different outcomes: insufficient permission, wrong state, missing data, concurrent conflicts. No need to enumerate error codes.

The assumptions section records defaults you adopted where requirements are silent. The split from `[NEEDS CLARIFICATION]`: **high-impact items where a wrong guess wastes work get a marker** (an answer is required); **low-impact items with a reasonable default go into assumptions** (confirmed in passing at review).

## Granularity

One Spec per feature, sized as "one complete task a user can finish".

```
docs/specs/
├── 訂單管理/
│   ├── 訂單建立.md
│   └── 訂單查詢.md
└── 會員/
    └── 會員註冊.md
```

When a topic has a single feature, place it directly at the topic level; no subdirectory for symmetry's sake.

## Self-check before delivery

Go through this after writing; it is the Spec's unit test:

**Requirement completeness**
- [ ] No `[NEEDS CLARIFICATION]` markers left (if any remain, list them explicitly for the user at delivery)
- [ ] Every FR is testable and unambiguous
- [ ] Every User Story has an independent test method and at least one acceptance scenario
- [ ] Success criteria are measurable
- [ ] Edge cases cover the main error branches

**SRS review**
- [ ] The SRS was read item by item for contradictions, conflicts with the current implementation, regulation/personal data, security, feasibility, and external dependencies
- [ ] Every risk found is in 「風險與疑慮」, each with concrete facts and a suggestion
- [ ] No risks invented to pad the list

**Content boundary**
- [ ] No class, method or file names
- [ ] No framework or package choices
- [ ] No table structures or field types
- [ ] Success criteria contain no technical terms

## Delivery

Write to the matching path under `docs/specs/`, then tell the user:

- The file location
- How many User Stories and FRs it covers
- **Every `[NEEDS CLARIFICATION]` marker, listed one by one for answering**
- **Every risk and concern, listed one by one** — especially those conflicting with regulation, security or the current implementation. This is the part of the delivery that most needs human decisions; state it in the conversation, not only in the file.
- The defaults adopted in the assumptions section

Then **stop and wait for review**. This is the flow's stage gate: no Plan without confirmation. Silence is not consent.

## The Spec is the single source of truth for current behavior

This is the flow's core agreement: **`docs/specs/` describes how the system behaves now, not what was once intended.**

A Spec is not a historical document archived once written. Anything that changes externally observable behavior must end up in the Spec:

- New features delivered in the SRS
- Requirement changes written only in work-item comments
- Behavior changed during implementation after confirming with the user
- Rules renegotiated while fixing a bug that revealed "what we agreed was wrong"

Whenever someone asks "how should the system respond in this situation", the answer must be in the Spec. If it is not, the Spec has fallen behind.

**The reverse holds too**: if the system cannot do what the Spec says, that is not "the document being idealistic" — it is a defect, either in the Spec or in the code, and someone must decide which.

### What stays out of the Spec

The boundary is **externally observable behavior**:

| Goes in the Spec | Stays out of the Spec |
| :--- | :--- |
| Behavior changes users can see and test | Fixing a crash that should never happen |
| New or changed acceptance conditions | Refactoring, renaming, performance tuning (behavior unchanged) |
| Business rules changed by agreement | Implementation approach, technology choices (that is the Plan) |

A bug fix that finally makes the code do what the Spec already says needs no Spec change — the Spec was right all along.

## Changing an existing Spec

Files under `docs/specs/`, like everything under `docs/`, are changed only with the user's consent. First explain what will change and why.

When an implemented feature's Spec disagrees with the code's actual behavior, report the gap and let the user decide — is the Spec wrong, or does the code have a bug? **Never rewrite the Spec to match the current code on your own**; that launders a defect into a specification.

When revising:

- Change the specific `FR-###` and acceptance scenarios; never rewrite the whole document — numbers must stay traceable from the Plan and tests.
- Issued numbers are never reordered; a dropped requirement is marked obsolete rather than reusing its number.
- Record the date, source (work-item number / SRS version) and change in the document's 「修訂紀錄」. Half a year later it must be clear whether an FR was original or agreed later.

## No Spec for engineering decisions

Work with no external business behavior (database migration strategy, external integration mechanics, cross-cutting infrastructure) goes straight to the Plan. Forcing User Stories and FRs onto it is ritual.

## Never gold-plate requirements

The Spec's scope is the SRS's scope. **Add no requirements of your own.**

While writing a Spec it is tempting to think "might as well add sorting" or "it should also export, right?" — these ideas may be right, but they were never confirmed, prioritized, or estimated by anyone. Every extra FR becomes implementation, test and maintenance cost.

The test: **every FR must trace back to some part of the SRS.** If it cannot, it should not exist.

If something genuinely seems missing, raise it in 「風險與疑慮」 (e.g. 「SRS 未提及列表排序，使用者可能預期有此功能」) and **let the user decide whether to include it**; never write it as an FR on your own.

Likewise, do not inflate the number of User Stories or stuff in edge cases to look complete. The shorter the Spec the better — as long as it covers everything the SRS requires.
