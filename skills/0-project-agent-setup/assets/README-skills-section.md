### AI 開發工具的 skills

專案 skills 放在 `.agents/skills/`（AGENTS.md）。Codex 與 Antigravity 直接讀這個目錄；Claude Code 只讀 `.claude/skills/`，clone 後執行一次腳本建立連結：

| 環境 | 指令 | 連結方式 |
| :--- | :--- | :--- |
| macOS、Linux、WSL | `scripts/link-skills.sh` | 相對路徑 symlink；WSL 且 repo 在 Windows 磁碟（`/mnt/c/...`）時，改由 Windows 建 junction |
| Windows（PowerShell 7） | `pwsh -File scripts\link-skills.ps1` | junction（不需系統管理員或開發人員模式） |
| Windows（Windows PowerShell 5.1） | `powershell -ExecutionPolicy Bypass -File scripts\link-skills.ps1` | 同上 |

- Windows 與 WSL 共用同一份 repo 時一律用 junction：WSL 看得到 junction，Windows 看不到 WSL 建立的 symlink。`link-skills.sh` 在這種情況會呼叫 Windows 的 `pwsh`（沒有則用 `powershell`）執行 `link-skills.ps1`。
- `.claude/skills` 連結各作業系統不同，不進版控（`.gitignore`）。
- Windows 的 junction 是絕對路徑，repo 搬移位置後重新執行。
