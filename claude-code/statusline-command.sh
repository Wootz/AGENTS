#!/bin/bash
# Claude Code status line
# Fields, in order: directory  git branch/status  model (effort) | context usage | rate limit

input=$(cat)

# 配色（256 色，深色背景）：一般狀態用藍/紫/綠，黃與紅只保留給警示
COL_DIR=$'\033[38;5;117m'     # 目錄：天藍
COL_GIT=$'\033[38;5;183m'     # git 分支：淡紫
COL_DIRTY=$'\033[38;5;216m'   # 有未提交變更 *：橘
COL_CLEAN=$'\033[38;5;114m'   # 乾淨 ✓：綠
COL_MODEL=$'\033[38;5;255m'   # 模型：白
COL_EFFORT=$'\033[38;5;248m'  # effort：灰
COL_OK=$'\033[38;5;114m'      # 用量正常：綠
COL_WARN=$'\033[38;5;220m'    # 用量警示：黃
COL_RED=$'\033[38;5;203m'     # 用量危險：紅
COL_SUBTLE=$'\033[38;5;240m'  # 分隔符號：深灰
RESET=$'\033[0m'

model=$(echo "$input" | jq -r '.model.display_name // empty')
effort=$(echo "$input" | jq -r '.effort.level // empty')
cwd=$(echo "$input" | jq -r '.workspace.current_dir // .cwd // empty')

# 1. Directory: show relative to $HOME with ~ when possible, else last segment
dir_display=""
if [ -n "$cwd" ]; then
  case "$cwd" in
    "$HOME") dir_display="~" ;;
    "$HOME"/*) dir_display="~${cwd#$HOME}" ;;
    *) dir_display="$cwd" ;;
  esac
fi

# 2. Git branch + dirty/clean status (skip optional locks, only if inside a git repo)
git_branch=""
git_status=""
if [ -n "$cwd" ] && git -C "$cwd" --no-optional-locks rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  git_branch=$(git -C "$cwd" --no-optional-locks branch --show-current 2>/dev/null)
  if [ -z "$git_branch" ]; then
    git_branch=$(git -C "$cwd" --no-optional-locks rev-parse --short HEAD 2>/dev/null)
  fi
  if [ -n "$(git -C "$cwd" --no-optional-locks status --porcelain 2>/dev/null)" ]; then
    git_status=" ${COL_DIRTY}*${RESET}"
  else
    git_status=" ${COL_CLEAN}✓${RESET}"
  fi
fi

# 區段分隔符號（context 與 rate limit 前方）
rl_sep="${COL_SUBTLE}|${RESET}"

# 4. Context usage (used %)
used=$(echo "$input" | jq -r '.context_window.used_percentage // empty')

parts=()

[ -n "$dir_display" ] && parts+=("${COL_DIR} ${dir_display}${RESET}")
if [ -n "$git_branch" ]; then
  parts+=("${COL_GIT}  ${git_branch}${RESET}${git_status}")
fi
if [ -n "$model" ]; then
  model_part="${COL_MODEL} ${model}${RESET}"
  [ -n "$effort" ] && model_part="${model_part} ${COL_EFFORT}(${effort})${RESET}"
  parts+=("$model_part")
fi

if [ -n "$used" ]; then
  used_int=$(printf '%.0f' "$used")
  ctx_color=$COL_OK
  [ "$used_int" -ge 80 ] && ctx_color=$COL_RED
  parts+=("${rl_sep} ${ctx_color}${used_int}%${RESET}")
fi

# 5. Rate limit（Claude.ai 訂閱：5 小時 / 7 天額度，各視窗獨立，缺少時略過）
# 格式：| 99%(55m) 13%(6d14h)（5 小時在前、7 天在後）；>=95% 紅色、>=80% 黃色，其餘綠色
now=$(date +%s)

# 將剩餘秒數轉成倒數文字：>=1 天顯示 d/h，>=1 小時顯示 h/m，否則 m
format_countdown() {
  local remain=$1 d h m
  d=$((remain / 86400))
  h=$(((remain % 86400) / 3600))
  m=$(((remain % 3600) / 60))
  if [ "$d" -ge 1 ]; then
    printf '%dd%dh' "$d" "$h"
  elif [ "$remain" -ge 3600 ]; then
    printf '%dh%dm' "$h" "$m"
  else
    printf '%dm' "$m"
  fi
}

# 參數：使用百分比、重置時間（epoch 秒）
rate_limit_part() {
  local pct=$1 resets=$2 pct_int color text remain
  [ -z "$pct" ] && return
  pct_int=$(printf '%.0f' "$pct")
  color=$COL_OK
  [ "$pct_int" -ge 80 ] && color=$COL_WARN
  [ "$pct_int" -ge 95 ] && color=$COL_RED
  text="${pct_int}%"
  if [ -n "$resets" ]; then
    resets=${resets%.*}
    remain=$((resets - now))
    # 重置時間已過則不顯示倒數
    [ "$remain" -gt 0 ] && text="${text}($(format_countdown "$remain"))"
  fi
  printf '%s' "${color}${text}${RESET}"
}

five_part=$(rate_limit_part \
  "$(echo "$input" | jq -r '.rate_limits.five_hour.used_percentage // empty')" \
  "$(echo "$input" | jq -r '.rate_limits.five_hour.resets_at // empty')")
week_part=$(rate_limit_part \
  "$(echo "$input" | jq -r '.rate_limits.seven_day.used_percentage // empty')" \
  "$(echo "$input" | jq -r '.rate_limits.seven_day.resets_at // empty')")

# 兩個視窗以空白相連，不加標籤（由位置辨識）
rl_part=""
[ -n "$five_part" ] && rl_part="$five_part"
[ -n "$week_part" ] && rl_part="${rl_part:+$rl_part }${week_part}"
[ -n "$rl_part" ] && parts+=("${rl_sep} ${rl_part}")

sep="${COL_SUBTLE} ${RESET}"
out=""
for i in "${!parts[@]}"; do
  if [ "$i" -eq 0 ]; then
    out="${parts[$i]}"
  else
    out="${out}${sep}${parts[$i]}"
  fi
done

printf "%s" "$out"
