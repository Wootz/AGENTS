# 量測方法

## 為什麼要量

肉眼對 10–20px 的差異幾乎無感，但客戶把設計稿和畫面並排看就會發現。
更麻煩的是**一致的錯誤**——整批元件都放大 1.5 倍時，畫面內部比例仍然協調，
看起來完全正常，只有量測才抓得到。

## 步驟一：判定畫布倍率

設計師常在放大的畫布上作業。動手量之前先確定倍率，否則後面全錯。

**方法：量一個已知寬度的元件回推。**

| 已知來源 | 說明 |
| --- | --- |
| Figma API 讀出的值 | 最可靠。`get_figma_data` 的 `dimensions.width` 是實際值 |
| 既有正確實作 | 該元件已對齊過，量瀏覽器實際渲染值 |
| 裝置寬度 | 手機框 390/393/414；設計稿常見 585(=390×1.5)、780(=390×2) |

```
設計稿量到 525px，已知實際應為 350px → 倍率 = 525 / 350 = 1.5
```

**驗算**：再量第二個元件確認同樣倍率。兩個都吻合才算確定。

算出後告訴使用者依據，例如：
> 這份稿的白卡量到 525px，Figma API 顯示應為 350px，推算畫布是 1.5×。
> 後續所有量到的值我都會除以 1.5。

同一批稿可能混用倍率——手機表單 1.5×、桌機頁 1×——**逐份判定**。

## 步驟二：掃描設計稿

用 `scripts/measure_design.py`：

```bash
# 找容器外框（白卡）——內部有輸入框等不同色元件時要加 --outer
python3 scripts/measure_design.py 稿.png --scan-y 400 --mode light --outer --scale 1.5

# 找一列多個元件（三個按鈕、六個驗證碼格）——會分別列出並算間隔
python3 scripts/measure_design.py 稿.png --scan-y 640 --mode mid --scale 1.5

# 不確定元件在哪條線上時，掃一段
python3 scripts/measure_design.py 稿.png --scan-range 300 360 --mode mid
```

| 參數 | 用途 |
| --- | --- |
| `--mode light` | 近白區塊（白卡、彈窗底） |
| `--mode mid` | 淺灰元件（輸入框、驗證碼格）——找表單元件最常用 |
| `--mode dark` | 深色元素（文字、深色按鈕） |
| `--outer` | 只取最外緣。**找容器一定要加**——卡片內部的輸入框會把區段切斷，不加會只量到圓角 |
| `--scale` | 畫布倍率，設定後同時輸出換算值 |

輸出含每個區段的**中心點**與**間隔**，用來判斷對齊方式與 gap。

也可以直接寫幾行 PIL：

```python
from PIL import Image
im = Image.open('figma/reg-02-form-empty.png').convert('RGB')
w, h = im.size
px = im.load()

y = 400  # 穿過白卡的橫線
xs = [x for x in range(w) if all(c > 250 for c in px[x, y])]
print('卡片', xs[0], xs[-1], '寬', xs[-1] - xs[0] + 1)
```

**求內距**：量外框與內部元件的差。
`(卡片寬 − 內部元件寬) / 2`，例如 `(436 − 372) / 2 = 32`。

**求對齊方式**：比中心點。若文字中心 ≈ 某元件中心，是置中對齊該元件；
若文字靠容器左緣，才是 `justify-between`。這個差別目測看不出來。

## 步驟三：驗證實作結果

改完之後用 Playwright 量實際渲染值。視窗尺寸要對應設計稿
（手機 390×844、桌機依畫布寬）。

```javascript
() => {
  const card = document.querySelector('SELECTOR');
  const inner = document.querySelector('INNER_SELECTOR');
  const c = card.getBoundingClientRect();
  const i = inner.getBoundingClientRect();
  const SCALE = 1.5;  // 設計稿倍率
  return {
    viewport: window.innerWidth,
    cardWidth: Math.round(c.width),
    designWidth: Math.round(525 / SCALE),      // 設計稿量到的值 ÷ 倍率
    innerPad: Math.round(i.left - c.left),
    designPad: Math.round(32 / SCALE),
    innerCentre: Math.round((i.left + i.right) / 2),
    cardCentre: Math.round((c.left + c.right) / 2),
  };
}
```

差 1px 可接受（四捨五入）；差 5px 以上要查原因。

## 需要量的項目

只量寬度不夠。逐項對照：

| 類別 | 要量／要比的 |
| --- | --- |
| 尺寸 | 容器寬高、內距、外距、元件間距（gap） |
| 對齊 | 中心點、左右緣、是置中還是兩端對齊 |
| 文字 | **逐字比對文案**、字級、字重、行高、顏色 |
| 顏色 | 背景、邊框、文字——用取色比對，不要沿用既有 token |
| 狀態 | 每個狀態（預設／輸入中／錯誤／停用／選中）分別比 |
| 圖示 | 尺寸、顏色、形狀（數 path 數量確認完整） |

**文案最容易被客戶抓到**，而且改起來最便宜。優先逐字核對。

## 取色

```python
from PIL import Image
im = Image.open('figma/xxx.png').convert('RGB')
px = im.load()
print('#%02X%02X%02X' % px[100, 200])  # 指定座標的色碼
```

取到的色若與專案既有 token 不同，**新增 token**，不要硬套相近的。
設計稿的 `#718096` 與專案的 `#5B6878` 都是灰色文字，但不是同一個。

## 截圖注意事項

**不要用 `fullPage: true`。** `position: fixed` 的元素在全頁截圖裡會被合成在
視窗位置，在一張比視窗高的圖裡就變成浮在中間，看起來像版面壞掉——
但實際捲動時完全正常。用視窗截圖，需要看下半部就先捲動再截。

截圖尺寸統一，讓使用者能並排比較。
