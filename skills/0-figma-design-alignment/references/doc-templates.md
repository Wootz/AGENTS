# Document Templates

The three documents each have a distinct purpose; keep them separate. Write them in Traditional Chinese (正體中文), as in the templates below.

| Document | Question it answers | Role when the client objects |
| --- | --- | --- |
| 設計稿存證 (design archive) | What did the design look like at the time? | After the designer revises, proves which version the implementation followed |
| 設計稿矛盾清單 (contradiction list) | Where does the design contradict itself? | Proves the implementation is not wrong; the design has no single answer |
| 畫面對照報告 (comparison report) | What changed, and what does it look like now? | Lets the user accept the work quickly |

---

## 1. 設計稿存證.md

```markdown
# 設計稿存證

<來源：客戶提供／Figma 匯出>，日期 <YYYY-MM-DD>，圖存於 `figma/`。
**這是 UI 的比對基準。**

矛盾與取捨見 [設計稿矛盾清單.md](./設計稿矛盾清單.md)；
實作後畫面見 [畫面對照_<功能>.md](./畫面對照_<功能>.md)。

## <功能名稱>

| # | 畫面 | 圖 |
| --- | --- | --- |
| 1 | <狀態說明> | ![](figma/<檔名>.png) |

### 本批確認的文案
- <欄位>：**<文案>**
- <錯誤情境>：**<文案>**

## 舊版（不採用）
![](figma/<檔名>-舊版.png)
<為什麼不採用、依誰的指示>
```

File naming: `<feature abbreviation>-<number>-<state>.png`, e.g. `reg-02-form-empty.png`. The state must be readable from the file name so the image can be found later.

---

## 2. 設計稿矛盾清單.md

```markdown
# 設計稿矛盾清單

同一功能有兩份以上設計稿、彼此不一致時記錄於此，並註明**採用哪一份、為什麼**。
客戶若對畫面有疑義，先看這裡——多數「不一致」是設計稿本身互相矛盾。

## <功能>

### 矛盾 N：<一句話描述> ⚠️

| 來源 | 說法 |
| --- | --- |
| <元件庫／某張稿> | <值> |
| **<實際採用的來源>** | **<值>** |

**採用：<哪個>**（<使用者確認日期>）。<理由>

> <風險或注意事項；若改用另一個要動到什麼>

---

## 刻意與設計稿不同之處（特殊需求）

**原則：畫面一律與設計稿一致**；以下是**經確認的例外**，變更前須再次確認。

### 1. <項目> ⚠️
<設計稿是什麼樣、實作是什麼樣>

**原因**：<為什麼刻意不同>。<誰在何時確認的>

---

## 待客戶提供
- <未定的內容、連結、資產>
```

**When you find a contradiction, ask the user which to use first**, then record it. The record holds the user's decision, not your judgment.

---

## 3. 畫面對照_<功能>.md

```markdown
# <功能> 畫面對照報告（<日期>）

- 設計稿原圖：[設計稿存證.md](./設計稿存證.md#<錨點>)
- 矛盾與取捨：[設計稿矛盾清單.md](./設計稿矛盾清單.md#<錨點>)

實際畫面以 Playwright 於 <寬×高>（設計稿畫布尺寸<，倍率 N×>）截圖。

## 一、本次修正的差異

| # | 項目 | 設計稿 | 修正前 | 修正後 |
| --- | --- | --- | --- | --- |
| 1 | <項目> | <值> | <值> | <值> |

修正檔案：`<路徑>`

## 二、修正後畫面

> ⚠️ 以下皆為**實際畫面**截圖，不是設計稿。設計稿原圖見上方連結。

### <狀態名稱>
🖥️ 實際畫面 ｜ 對照 `figma/<檔名>.png`

![實際畫面：<狀態>](screenshots/<檔名>.png)

<這張要看什麼、與設計稿的關係>

## 三、需要你確認或決策的項目

### 1. <項目> ⚠️
<問題描述、影響、為什麼沒自己決定>

## 四、驗證結果

- build 通過／測試 <n> 項全綠
- 量測驗證：<項目> 設計稿 <值> ↔ 實際 <值> ✅

<未執行的驗證與原因>
```

### Writing notes

**Label every screenshot 「🖥️ 實際畫面」.** A report with only implementation screenshots captioned 「對照 Figma xxx」 makes readers think some of them are the design. Label clearly and point to the design file name.

**The fix table needs a design column.** "Before → after" alone cannot prove the fix is right.

**Explain why each open item was not decided by you**; otherwise it reads as skipping work.

**Put measured numbers in the verification results**, not just 「已對齊」.
