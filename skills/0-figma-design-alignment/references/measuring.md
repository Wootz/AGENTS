# Measuring

## Why measure

The eye barely registers 10–20px differences, but the client spots them with the design and the screen side by side. Worse is a **consistent error** — when every element is 1.5× too big, internal proportions still look balanced and the screen seems entirely normal; only measurement catches it.

## Step 1: Determine the canvas scale

Designers often work on an enlarged canvas. Settle the scale before measuring, or everything after is wrong.

**Method: measure an element of known width and work back.**

| Known source | Notes |
| --- | --- |
| Value from the Figma API | Most reliable. `dimensions.width` from `get_figma_data` is the real value |
| An existing correct implementation | The element is already aligned; measure its rendered value in the browser |
| Device width | Phone frames 390/393/414; designs commonly 585 (=390×1.5) or 780 (=390×2) |

```
Design measures 525px, known real value 350px → scale = 525 / 350 = 1.5
```

**Cross-check**: measure a second element to confirm the same scale. Only two matches make it certain.

Then tell the user the basis, e.g.:
> 這份稿的白卡量到 525px，Figma API 顯示應為 350px，推算畫布是 1.5×。後續所有量到的值我都會除以 1.5。

One set may mix scales — phone forms 1.5×, desktop pages 1× — so **determine it per file**.

## Step 2: Scan the design

Use `scripts/measure_design.py`:

```bash
# Container outline (white card) — add --outer when it holds differently colored elements such as inputs
python3 scripts/measure_design.py design.png --scan-y 400 --mode light --outer --scale 1.5

# A row of elements (three buttons, six OTP boxes) — lists each one and the gaps
python3 scripts/measure_design.py design.png --scan-y 640 --mode mid --scale 1.5

# Unsure which line crosses the element: scan a band
python3 scripts/measure_design.py design.png --scan-range 300 360 --mode mid
```

| Option | Purpose |
| --- | --- |
| `--mode light` | Near-white areas (white cards, dialog backgrounds) |
| `--mode mid` | Light-gray elements (inputs, OTP boxes) — the usual choice for form elements |
| `--mode dark` | Dark elements (text, dark buttons) |
| `--outer` | Outer edges only. **Always use it for containers** — inputs inside a card split the segment, and without it you measure only the rounded corner |
| `--scale` | Canvas scale; when set, scaled values are printed too |

The output includes each segment's **center** and the **gaps**, used to judge alignment and gap.

A few lines of PIL also work:

```python
from PIL import Image
im = Image.open('figma/reg-02-form-empty.png').convert('RGB')
w, h = im.size
px = im.load()

y = 400  # a line crossing the white card
xs = [x for x in range(w) if all(c > 250 for c in px[x, y])]
print('card', xs[0], xs[-1], 'width', xs[-1] - xs[0] + 1)
```

**Padding**: the difference between the outline and the inner element: `(card width − inner width) / 2`, e.g. `(436 − 372) / 2 = 32`.

**Alignment**: compare centers. If the text center ≈ an element's center, it is centered on that element; only if the text hugs the container's left edge is it `justify-between`. The eye cannot tell these apart.

## Step 3: Verify the implementation

After the change, measure rendered values with Playwright, with the viewport matching the design (phone 390×844, desktop per canvas width).

```javascript
() => {
  const card = document.querySelector('SELECTOR');
  const inner = document.querySelector('INNER_SELECTOR');
  const c = card.getBoundingClientRect();
  const i = inner.getBoundingClientRect();
  const SCALE = 1.5;  // design canvas scale
  return {
    viewport: window.innerWidth,
    cardWidth: Math.round(c.width),
    designWidth: Math.round(525 / SCALE),      // measured design value ÷ scale
    innerPad: Math.round(i.left - c.left),
    designPad: Math.round(32 / SCALE),
    innerCentre: Math.round((i.left + i.right) / 2),
    cardCentre: Math.round((c.left + c.right) / 2),
  };
}
```

1px off is acceptable (rounding); 5px or more needs investigating.

## What to measure

Width alone is not enough. Compare item by item:

| Category | Measure / compare |
| --- | --- |
| Size | Container width/height, padding, margin, gaps between elements |
| Alignment | Centers, left/right edges, centered vs space-between |
| Text | **Copy word for word**, font size, weight, line height, color |
| Color | Background, border, text — pick colors; never reuse an existing token by default |
| States | Each state (default / typing / error / disabled / selected) separately |
| Icons | Size, color, shape (count paths to confirm completeness) |

**Copy is what the client catches most**, and it is the cheapest to fix. Check it word for word first.

## Picking colors

```python
from PIL import Image
im = Image.open('figma/xxx.png').convert('RGB')
px = im.load()
print('#%02X%02X%02X' % px[100, 200])  # color at the given coordinate
```

When a picked color differs from the project's existing tokens, **add a token** instead of forcing the nearest one. The design's `#718096` and the project's `#5B6878` are both gray text, but not the same.

## Screenshot notes

**Avoid `fullPage: true`.** `position: fixed` elements are composited at the viewport position in full-page shots; in an image taller than the viewport they float mid-page and the layout looks broken — although scrolling the real page is fine. Use viewport screenshots; scroll first to capture the lower part.

Keep screenshot sizes uniform so the user can compare side by side.
