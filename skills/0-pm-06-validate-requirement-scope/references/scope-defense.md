# Scope defense rules

The single source for traffic-light classification, warning keywords and response scripts. Used by `0-pm-03-generate-interview-outline`, `0-pm-04-generate-mom`, `0-pm-06-validate-requirement-scope` and `0-pm-07-generate-change-request`.

Scripts are client-facing and stay in Traditional Chinese.

## Scope baseline

The baseline is what both parties agreed in writing:

- The latest SOW version in `docs/sow/` — in-scope `SOW-2.1.x`, exclusions `SOW-2.2.x`
- CRs in `docs/change-requests/` with status 已核准 (client chose Option A)
- The latest SRS version in `docs/srs/`, when one exists — `REQ-{MODULE}-NNN`

Signing usually happens on paper, so the files often still say 草稿. Use the latest version anyway; when the SOW or SRS used is not 已簽署, say 「基準文件尚未標記為已簽署」 at the top of the output.

Verbal promises and emails with no matching document are not baseline.

## Traffic light

Classify in this order and stop at the first match:

1. **Explicitly excluded** (SOW 2.2) → 🔴
2. **Explicitly included** by a baseline item → 🟢
3. **A related baseline item exists but does not settle it** (two readings are possible) → 🟡
4. **Nothing in the baseline relates to it** → 🔴 (deny by default)

When the SOW both includes and excludes it, classify 🟡 and report the contradiction.

🟢 always cites the exact clause: `[Ref: SOW-2.1.3]`, `[Ref: REQ-LOGIN-001]`, `[Ref: CR-20261110-01]`. No citable clause means it is 🟡 or 🔴.

| Light | Recorded with it | What happens next |
| :--- | :--- | :--- |
| 🟢 In scope | The cited clause | Goes into the SRS (or is already there) |
| 🟡 Gray area | The clause, both readings, the PM who rules, a ruling date 2 working days out | PM rules in writing (MOM or email): 🟢 → SRS; 🔴 → CR |
| 🔴 Out of scope | Why: excluded by which clause, or not covered | Frozen — no development until a CR is signed. Offer Phase 2 or a CR |

Examples:

| Request | Baseline | Light |
| :--- | :--- | :--- |
| 密碼錯誤 3 次鎖定 15 分鐘 | SOW-2.1.1 會員登入（含安全機制） | 🟢 `[Ref: SOW-2.1.1]` |
| 登入頁加「記住我」 | SOW-2.1.1 只寫「會員登入」 | 🟡 記住帳號 vs. 免密碼自動登入 |
| 使用者清單匯出 Excel | SOW-2.2.4 不包含報表功能 | 🔴 明確排除 |

## Warning keywords

These phrases from a client signal scope creep. Flag every occurrence, classify the request before anything is agreed, and suggest the matching script.

| Client says | Usually means | Script |
| :--- | :--- | :--- |
| 「順便…」 | A free addition | 🔴 Option B (Phase 2 or CR) |
| 「應該要有…」 | Unwritten scope assumed included | Back to the SOW |
| 「也可以…」 | Soft probing; creep is starting | Classify, then 🟡 or 🔴 script |
| 「改一下…」 | Effort underestimated | Effort estimate first |
| 「這很快吧？」 | Pressure for free work | Effort estimate first; keep the CR process |

## Response scripts

Fill every `{{…}}`; send nothing with a placeholder left in it.

**🟢 In scope**

> 「沒問題！『{{功能名稱}}』在 SOW 第 {{條款}} 條的範圍內，我們會納入 {{MOM／SRS}}。」

**🟡 Gray area**

> 「謝謝您的建議。『{{功能名稱}}』的範圍需要再確認，請問您指的是：
> A) {{解讀 A}}？
> B) {{解讀 B}}？
> 我們會在 {{日期}} 前給您明確回覆。」

**🔴 Option A — defer to Phase 2**

> 「感謝您的建議！『{{功能名稱}}』目前不在我們簽訂的 SOW 範圍內。此功能屬於擴充性質，建議納入 Phase 2：
> ✅ 確保核心系統如期於 {{上線日}} 上線
> ✅ Phase 2 可依一期上線後的實際使用情況調整需求
> ✅ 避免本期臨時變更影響現有功能穩定性
> 您覺得這樣處理可以嗎？」

**🔴 Option B — Phase 2 or CR**

> 「感謝您的建議！『{{功能名稱}}』目前不在我們簽訂的 SOW 範圍內。我們有兩個選項：
> 1️⃣ 記錄為 Phase 2 規劃（不影響本期時程與人天）
> 2️⃣ 啟動變更請求評估：預估 {{人天}} 人天，時程延後 {{天數}} 天
> 請問您希望如何處理？」

With no estimate yet (e.g. live in a meeting), line 2️⃣ becomes: 「2️⃣ 啟動變更請求評估：我們會在 {{日期}} 前提供人天與時程影響評估」.

**Back to the SOW** — when the client insists it should be included

> 「我理解您的期待。我們回到 SOW 確認：第 {{條款}} 條定義的範圍是『{{原文}}』。
> 『{{功能名稱}}』確實有價值，但不在雙方簽署的範圍內。為了保護雙方權益，建議透過正式變更請求（CR）流程處理，讓您得到完整規劃，我們也能配置足夠資源妥善實作。」

**Effort estimate first**

> 「讓我先評估工作量與時程影響，{{日期}} 前給您正式評估，確保品質不受影響。」
