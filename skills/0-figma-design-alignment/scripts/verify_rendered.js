/**
 * 量實際渲染畫面，與設計稿換算值比對。
 *
 * 改完就宣告完成是這個流程最常見的失敗——改的時候算對了，實際渲染出來
 * 因為 padding 疊加、flex 壓縮又是另一回事。這個腳本用來收尾驗證。
 *
 * 用法：貼進 Playwright 的 browser_evaluate，改 TARGETS 與 SCALE。
 * 視窗尺寸要先設成設計稿對應的尺寸（手機 390×844、桌機依畫布寬）。
 */
() => {
  // ── 依實際情況修改 ────────────────────────────────────────────
  const SCALE = 1.5; // 設計稿畫布倍率（1× 桌機稿填 1）

  const TARGETS = {
    // 名稱: { sel: CSS 選擇器, design: 設計稿量到的原始值（未除以倍率） }
    card: { sel: 'SELECTOR_HERE', design: { width: 525 } },
    input: { sel: 'SELECTOR_HERE', design: { width: 461 } },
  };

  // 要驗證「A 是否置中對齊 B」時填這裡
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
      // 差 1px 內視為四捨五入誤差；超過就要查
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
    // 置中對齊時兩者中心應一致；差很多代表用錯了排版方式
    // （例如該置中卻用了 justify-between）
    if (Math.abs(diff) > 2) out.issues.push(`${g.name} 中心差 ${diff}px——是否誤用 justify-between？`);
  }

  out.verdict = out.issues.length === 0 ? '✅ 全部符合' : `⚠️ ${out.issues.length} 項不符`;
  return out;
};
