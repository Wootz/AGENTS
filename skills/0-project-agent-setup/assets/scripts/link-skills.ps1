# 建立 .claude\skills -> .agents\skills 連結，讓 Claude Code 讀到專案 skills（Windows PowerShell 5.1、PowerShell 7）。
# Windows 建 junction：不需要系統管理員或開發人員模式，WSL 也看得到；repo 搬移位置後請重新執行。
# macOS、Linux 的 PowerShell 7 建相對路徑 symlink（同 link-skills.sh）。
# Codex 與 Antigravity 直接讀 .agents\skills，不需要連結。連結不進版控（.gitignore）。
$ErrorActionPreference = 'Stop'
# 由 WSL 的 link-skills.sh 呼叫時，輸出以 UTF-8 回傳，避免中文變亂碼
[Console]::OutputEncoding = [Text.UTF8Encoding]::new()

$root = Split-Path -Parent $PSScriptRoot
$link = Join-Path $root '.claude\skills'
$target = Join-Path $root '.agents\skills'

if (Test-Path -LiteralPath $link) {
    $item = Get-Item -LiteralPath $link -Force
    if (-not ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "$link 已存在且不是連結，請先移除後再執行。"
    }
    $item.Delete()
}

New-Item -ItemType Directory -Force -Path (Join-Path $root '.claude') | Out-Null
if ($PSVersionTable.PSEdition -eq 'Core' -and -not $IsWindows) {
    New-Item -ItemType SymbolicLink -Path $link -Target '../.agents/skills' | Out-Null
}
else {
    New-Item -ItemType Junction -Path $link -Target $target | Out-Null
}
Write-Output "已建立 $link -> $target"
