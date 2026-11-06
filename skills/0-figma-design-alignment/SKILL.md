---
name: 0-figma-design-alignment
description: Align a frontend screen with its Figma design and leave documentation that can serve as evidence for the client. Use it whenever the user talks about matching the design, the screen differing from Figma, the client saying the screen is wrong, comparing against the design, the UI not matching, or changing the UI to follow the design (「對齊設計稿」「跟 Figma 不一樣」「客戶說畫面不對」「設計稿比對」「UI 跟設計不符」「照設計稿改」), and when the user pastes design screenshots or a Figma link, or complains about gaps between screen and design. Covers getting the design (Figma MCP or user screenshots), pixel-level measurement, SVG icon hand-off, archiving the design as evidence, recording contradictions, and a screenshot acceptance report after the fix.
---

# Align with the Figma Design

When the client says "the screen doesn't match the design", the hard part is not the CSS but three things:

1. **Eyeballing lies.** A card 12px too wide or text shifted 100px both look "about right".
2. **Designs change.** The designer revises the file, the client brings the new version and says you got it wrong, and you have no evidence of what you worked from.
3. **Designs contradict themselves.** Two frames for the same feature carry different copy; whichever you follow, you are wrong.

This skill replaces eyeballing with measurement, archives the design as it was, and lays contradictions open for the client to decide.

All documents this skill produces (under `docs/design/`) are written in Traditional Chinese (正體中文), following `references/doc-templates.md`.

## Core principles

**The screen must match the design.** Every intentional deviation is written into the documents with its reason — an undocumented deviation later becomes "you did it wrong".

**When in doubt, ask the user.** For contradicting designs, conflicting specs, or copy with two versions, your judgment may be right, but the decision is not yours. Lay out the options and their impact and let the user choose. The only exception is a pure implementation detail (flex vs grid) that does not affect the look.

## Workflow

### 1. Get the design

Try Figma MCP first (`mcp__Figma__get_figma_data`). It returns **exact spec values** — colors, spacing, font size, weight — more reliable than measuring an image.

When MCP fails (quota exhausted, no permission, no MCP), ask the user for screenshots. Say exactly which screens are needed: every state (empty, filling in, error, success, disabled) — a missing state means that state's styles are missed.

> Figma's quota is tied to the **account**, not the token. On a 429, switching tokens does nothing; it takes an account with a paid seat, or waiting for the quota reset (retry-after can exceed 60 hours). Tell the user this directly so they don't waste time swapping tokens.

### 2. Archive the evidence (before changing anything)

Save the design images to `docs/design/figma/`, with file names that show the screen and state (`reg-02-form-empty.png`, `sso-05-otp-error-wrong.png`).

Never skip this step. After the designer revises the file, these images are the only proof of which version you worked from. Also create `docs/design/設計稿存證.md` as the index, with source and date.

### 3. Determine the canvas scale ⚠️ the easiest step to get wrong

Design pixel values **are not CSS values**. Designers often work on an enlarged canvas — a 585px-wide frame may correspond to a 390px phone (1.5×); a 2000px frame may be desktop at 1×.

Method: measure a known element and work back. If the white card measures 525px in the design and the Figma API or an existing implementation says it should be 350px, the scale is 525 ÷ 350 = 1.5.

**Tell the user how you derived the scale** before continuing. A wrong scale makes every size wrong — consistently and plausibly wrong, which is the hardest kind of error to notice.

One set of designs may mix scales (form pages 1.5×, desktop login 1×); confirm each file.

### 4. Measure and compare

`scripts/measure_design.py` scans design pixels to find element edges. See `references/measuring.md`.

Divide every measured value by the scale to get the target CSS value.

Compare more than sizes: **copy word for word, font size, weight, color, spacing, icons, component states**. A one-character copy difference (「不透漏」 vs 「不願透漏」) is the first thing the client sees.

### 5. Hand icon export to the user

Never draw an approximate icon yourself. However close, it differs, and it cannot be defended as evidence.

Create the files in `frontend/public/assets/icons/` (or the project's equivalent), with a first-line comment stating purpose, size and color, then **give the user the paths and ask them to export from Figma and paste in**.

On receipt, **count the `<path>` elements**. Exports often miss inner layers — only the outer circle, without the exclamation mark or check inside. If so, ask the user to re-export.

One exception: an element like a white spinner on a dark button becomes invisible when the export hard-codes its color. Keep a `currentColor` inline SVG and explain why in a comment.

### 6. Re-measure after the change

Declaring done right after the edit is this workflow's most common failure. After the change:

Open the real screen with Playwright, measure with `getBoundingClientRect()`, and compare item by item against the scaled design values. **"Looks right" is not verification.**

Set the viewport to match the design (phone 390×844, desktop per the design canvas).

### 7. Screenshots and report

Take one screenshot per state into `docs/design/screenshots/` and write a comparison report (see `references/doc-templates.md`). The report is for the user to **confirm quickly**, so:

- Fixes go in a table: item｜design｜before｜after
- Label every screenshot 「🖥️ 實際畫面」 with the design file it is compared against — otherwise the user cannot tell which image is the design and which is yours
- Keep screenshot sizes uniform and **avoid fullPage**: `position: fixed` elements are composited at the viewport position in full-page shots and appear to float mid-page, which misleads

### 8. Record contradictions and exceptions

Two documents with different purposes:

**Design contradiction list (設計稿矛盾清單)** — where the design disagrees with itself. List each version's wording, which one was adopted, and why. This is the shield when the client objects: the implementation is not wrong; the design has no single answer. When you find a contradiction, **ask the user which to use first**.

**Intentional exceptions** — places that knowingly differ from the design, with the reason. For example a field the design lacks (but the backend needs), or a button clickable in the design but disabled because the feature is unfinished.

## Output structure

```
docs/design/
├── README.md              # 各檔用途、畫布倍率換算表
├── FIGMA.md               # Figma 連結＋API 讀出的規格值
├── 設計稿存證.md           # 設計稿原圖索引（來源、日期）
├── 設計稿矛盾清單.md        # 矛盾＋採用理由；刻意例外
├── 畫面對照_<功能>.md       # 修正表＋實際畫面截圖
├── figma/                 # 設計稿原圖（存證）
└── screenshots/           # 實作後的實際畫面
```

When the project already has its own documentation conventions, ask the user where these go.

## Common mistakes

Each of these has actually happened:

**Using enlarged-canvas pixels as CSS values** — everything comes out 1.5× too big, but uniformly so, and looks plausible. Determine the scale first.

**Not re-measuring after the change** — the numbers were right while editing, but the rendered result differs (padding stacking, flex shrinking).

**Implementing "left and right apart" with justify-between** — in the design, the text is **centered on some element** while another element sits right; that is not space-between. Measuring the two center points shows the difference.

**Full-page screenshot artifacts** — fixed elements land in the wrong place in fullPage shots and make the layout look broken. Use viewport screenshots.

**Reusing an existing token instead of checking the real color** — the design's `#718096` and the project's `#5B6878` are both "gray text" but not the same. Pick each color; add a token when it differs.

**Assuming the component library matches the actual page** — they can contradict (library says 「不願透漏」, page says 「不透漏」). Follow the actual page, but confirm with the user.

## References

- `references/measuring.md` — measuring method, scale determination, verification script usage
- `references/doc-templates.md` — templates and writing guidance for the three documents
- `scripts/measure_design.py` — scans a design image for element edges
- `scripts/verify_rendered.js` — measures the real screen in the browser (for Playwright evaluate)
