# AI Agent 全域規範與規格庫 (`~/.agents`)

這個專案用來集中管理我所有電腦上的 AI Agent（包含 Antigravity CLI、Codex 和 Claude Code）的最高全域架構規範、多語言技術棧要求、以及 UI/UX 一致性紀律。

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
