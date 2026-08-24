# AI Agent 全域規範與規格庫 (`~/.agents`)

這個私有專案用來集中管理我所有電腦上的 AI Agent（包含 GitHub Copilot、Claude Code、Aider 和 Cursor）的最高全域架構規範、多語言技術棧要求、以及 UI/UX 一致性紀律。

## 安裝非自建的 Skill

`skills/` 目錄下除了少數自建的 skill 之外，其餘皆透過 `.skill-lock.json` 記錄的來源安裝。在新電腦上 clone 此 repo 後，執行以下指令即可依照 `.skill-lock.json` 安裝所有非自建的 skill：

```bash
npx skills update
```
