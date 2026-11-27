/**
 * Measure the rendered screen and compare with the scaled design values.
 *
 * Declaring done right after the edit is this workflow's most common failure —
 * the numbers were right while editing, but rendering differs (padding stacking,
 * flex shrinking). This script is the final verification. Result messages are
 * in Traditional Chinese (human-facing).
 *
 * Usage: paste into Playwright's browser_evaluate and edit TARGETS and SCALE.
 * Set the viewport to the design's size first (phone 390×844, desktop per canvas width).
 */
() => {
  // ── Edit for the case at hand ─────────────────────────────────
  const SCALE = 1.5; // design canvas scale (1 for a 1× desktop design)

  const TARGETS = {
    // name: { sel: CSS selector, design: raw values measured in the design (not divided by the scale) }
    card: { sel: 'SELECTOR_HERE', design: { width: 525 } },
    input: { sel: 'SELECTOR_HERE', design: { width: 461 } },
  };

  // Fill in to check whether A is centered on B
  const ALIGN = [
    // { name: '倒數對齊方格', a: 'SEL_A', b: 'SEL_B' },
  ];
  // ──────────────────────────────────────────────────────────────

  const round = (n) => Math.round(n * 10) / 10;
  const out = { viewport: window.innerWidth, scale: SCALE, sizes: {}, align: [], issues: [] };

  for (const [name, t] of Object.entries(TARGETS)) {
    const el = document.querySelector(t.sel);
    if (!el) {
      out.issues.push(`找不到元素：${name} (${t.sel})`);
      continue;
    }
    const r = el.getBoundingClientRect();
    const row = { actualWidth: round(r.width), actualHeight: round(r.height) };
    if (t.design?.width) {
      row.designWidth = round(t.design.width / SCALE);
      row.diff = round(r.width - t.design.width / SCALE);
      // within 1px is rounding; more needs investigating
      if (Math.abs(row.diff) > 1) out.issues.push(`${name} 寬度差 ${row.diff}px`);
    }
    out.sizes[name] = row;
  }

  for (const g of ALIGN) {
    const a = document.querySelector(g.a);
    const b = document.querySelector(g.b);
    if (!a || !b) {
      out.issues.push(`對齊檢查找不到元素：${g.name}`);
      continue;
    }
    const ra = a.getBoundingClientRect();
    const rb = b.getBoundingClientRect();
    const ca = (ra.left + ra.right) / 2;
    const cb = (rb.left + rb.right) / 2;
    const diff = round(ca - cb);
    out.align.push({ name: g.name, centreA: round(ca), centreB: round(cb), diff });
    // Centered elements share a center; a large difference means the wrong layout
    // (e.g. justify-between where centering was intended)
    if (Math.abs(diff) > 2) out.issues.push(`${g.name} 中心差 ${diff}px——是否誤用 justify-between？`);
  }

  out.verdict = out.issues.length === 0 ? '✅ 全部符合' : `⚠️ ${out.issues.length} 項不符`;
  return out;
};
