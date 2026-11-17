#!/usr/bin/env bash
# 建立 .claude/skills -> .agents/skills 連結，讓 Claude Code 讀到專案 skills（macOS、Linux、WSL）。
# WSL 且 repo 在 Windows 磁碟（/mnt/<磁碟>/）時，改由 Windows 執行 link-skills.ps1 建 junction：
# Windows 看不到 WSL 建立的 symlink，WSL 看得到 junction。
# Codex 與 Antigravity 直接讀 .agents/skills，不需要連結。連結不進版控（.gitignore）。
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
link="$root/.claude/skills"

if [ -n "${WSL_DISTRO_NAME:-}" ] && [[ "$root" == /mnt/[a-z]/* ]]; then
  if command -v pwsh.exe >/dev/null 2>&1; then
    ps=(pwsh.exe)
  else
    ps=(powershell.exe -ExecutionPolicy Bypass)
  fi
  exec "${ps[@]}" -NoProfile -File "$(wslpath -w "$root/scripts/link-skills.ps1")"
fi

if [ -e "$link" ] && [ ! -L "$link" ]; then
  echo "$link 已存在且不是連結，請先移除後再執行。" >&2
  exit 1
fi

mkdir -p "$root/.claude"
ln -sfn ../.agents/skills "$link"
echo "已建立 $link -> ../.agents/skills"
