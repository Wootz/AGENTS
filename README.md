# AI Agent 全域規範與規格庫 (`~/.agents`)

這個專案用來集中管理我所有電腦上的 AI Agent（包含 Antigravity CLI、Codex 和 Claude Code）的最高全域架構規範、多語言技術棧要求、以及 UI/UX 一致性紀律。

## 自建 Skill

自建 skill 一律以 `0-` 開頭（`.gitignore` 只納管 `skills/0-*`）；同一系列加上系列名與順序編號。

### 開發前：專案管理（`0-pm-*`，依編號順序使用）

文件存放於專案的 `docs/` 下；紅綠燈判定與防禦話術共用 `0-pm-06-validate-requirement-scope/references/scope-defense.md`。

| Skill | 用途 | 產出 |
| :--- | :--- | :--- |
| `0-pm-01-generate-sow` | 工作說明書：編號的交付項目與排除項目、依賴、客戶配合事項、里程碑 | `docs/sow/` |
| `0-pm-02-generate-risk-assessment` | 專案風險評估：機率 × 衝擊、緩解與應變、負責人、監控觸發條件 | `docs/risks/` |
| `0-pm-03-generate-interview-outline` | 需求訪談大綱：對照 SOW 的深水區問題與現場應對話術 | `docs/interviews/` |
| `0-pm-04-generate-mom` | 會議紀錄：每項需求做 🟢🟡🔴 範疇判定，整理待辦與待追問 | `docs/meetings/` |
| `0-pm-05-generate-srs` | 給客戶簽核的需求規格書：每條需求追溯至 SOW 或 CR，驗收標準可判定 Pass／Fail | `docs/srs/` |
| `0-pm-06-validate-requirement-scope` | 客戶臨時提出需求時即時判定範圍，附回覆客戶話術 | 對話輸出 |
| `0-pm-07-generate-change-request` | 變更請求單：落差分析、人天與時程影響、三個處理選項 | `docs/change-requests/` |

### 開發：SRS → Spec → Plan → Code → Verify

| Skill | 用途 | 產出 |
| :--- | :--- | :--- |
| `0-generate-spec` | 依 SRS 寫功能規格（Spec Kit 格式），只寫 WHAT／WHY | `docs/specs/` |
| `0-generate-plan` | 依已核准的 Spec 寫實作計畫與任務清單 | `docs/plans/` |
| `0-generate-system-design` | 系統設計規格書（arc42 精簡版），結案或功能完成後同步 | `docs/architecture/` |

### 專案建置與工具

| Skill | 用途 |
| :--- | :--- |
| `0-project-agent-setup` | 建立或更新專案 `AGENTS.md` 與 `.agents/skills/` 實體副本；依專案選擇當下來源，匯出名稱移除 `0-`。專案規格使用 `generate-spec`，`to-spec` 留供本機個人使用 |
| `0-dotnet-project-setup` | 從零建立 .NET 後端專案（Clean Architecture、CQRS、CPM） |
| `0-dotnet-vscode-debug-setup` | 設定 ASP.NET Core + SPA 的 VS Code 偵錯（F5 啟動、中斷點） |
| `0-spa-backend-single-site` | 前端 SPA 併入 ASP.NET Core，單一站台部署 |
| `0-azure-appservice-deploy-script` | 以發行設定檔產生一鍵部署到 Azure App Service 的腳本 |
| `0-figma-design-alignment` | 畫面對齊 Figma 設計稿，並留下比對證據 |
| `0-package-compliance` | 檢查套件授權、版本、發布天數與維護狀態是否符合 AGENTS.md |

## 安裝非自建的 Skill

`skills/` 目錄下除了少數自建的 skill 之外，其餘皆透過 `.skill-lock.json` 記錄的來源安裝。在新電腦上 clone 此 repo 後，執行以下指令即可依照 `.skill-lock.json` 安裝所有非自建的 skill：

```bash
npx skills update
```

## 新增 Skill

指定全域安裝與實際使用的 agent（`antigravity-cli`、`codex`、`claude-code`），安裝時就不會詢問：

```bash
npx skills add <package> -g -a antigravity-cli codex claude-code -y
```

- `-y` 會安裝套件內的所有 skill；只裝特定 skill 時加 `-s <skill 名稱>`，可用 `-l` 列出套件內的 skill。

## 建立 Claude Code 與 Codex 的 Symlink

讓 Claude Code 與 Codex 讀取此 repo 的全域規範與 skill：

```bash
mkdir -p ~/.claude ~/.codex
ln -s ~/.agents/AGENTS.md ~/.claude/CLAUDE.md
ln -s ~/.agents/skills ~/.claude/skills
ln -s ~/.agents/AGENTS.md ~/.codex/AGENTS.md
ln -s ~/.agents/skills ~/.codex/skills
```
