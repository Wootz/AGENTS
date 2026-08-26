---
name: 0-spa-backend-single-site
description: 將獨立的前端 SPA（Vue/React/Vite）與 ASP.NET Core 後端整合成單一站台部署，讓 dotnet publish 自動建置前端並收進 wwwroot。當使用者提到「前後端整合」「只想部署一個站台」「合併成單一站台」「前端放進 wwwroot」「不想分開部署前後端」「單一 App Service」，或詢問如何讓後端同時提供 API 與前端靜態檔時，務必使用此技能。即使使用者只說「整合前端」或「一個站台就好」而未細講技術細節，也應該觸發。
---

# 前後端整合為單一站台

把原本分開跑的 SPA（Vite dev server）與 ASP.NET Core API 合併成一個站台：後端同時提供 API 與前端靜態檔，部署時只需要一個 App Service / 容器。

## 為什麼這件事容易做壞

整合看起來只是「把 dist 複製到 wwwroot」，但實際上有四個地方會在部署後才爆炸，而且本機開發模式完全看不出來。先掃描這四項，再動手改。

## 步驟一：先偵查，別急著改

整合前務必確認以下幾點，它們決定了要改哪些檔案：

```bash
# 前端硬編碼的後端位址（最常見的地雷）
grep -rn "localhost:[0-9]\+\|ws://\|wss://\|baseURL\|VITE_" frontend/src/

# 路由模式：createWebHistory 需要 SPA fallback，createWebHashHistory 不用
grep -rn "createWebHistory\|createWebHashHistory\|BrowserRouter\|HashRouter" frontend/src/

# 後端是否已有佔用根路徑的端點
grep -n 'MapGet("/"' backend/**/Program.cs
```

同時讀 `vite.config.ts`（看既有 proxy 設定）與 `Program.cs`（看中介軟體順序）。

## 步驟二：修正前端的同源連線

**WebSocket 是最常被漏掉的一項。** API 呼叫通常已經走相對路徑（靠 Vite proxy），但 WebSocket 常被硬編碼成 `ws://localhost:5082`。整合後前後端同源，必須改用 `window.location.host`：

```ts
const wsUrl = computed(() => {
  const loc = window.location
  const proto = loc.protocol === 'https:' ? 'wss:' : 'ws:'
  // 同源連線：正式部署時前後端同一站台，開發時由 Vite 的 /ws proxy 轉發到後端。
  return `${proto}//${loc.host}/ws`
})
```

改成同源後，開發模式（Vite :5173）就需要 proxy 才能連到後端，所以 `vite.config.ts` 要補上 `/ws`（`ws: true` 是必要的，否則不會升級成 WebSocket 連線）：

```ts
server: {
  proxy: {
    '/api': { target: 'http://localhost:5082', changeOrigin: true, secure: false },
    // WebSocket 已改為同源連線，開發模式需由 dev server 轉發到後端。
    '/ws': { target: 'ws://localhost:5082', ws: true, changeOrigin: true, secure: false }
  }
}
```

## 步驟三：後端提供靜態檔與 SPA fallback

在 `Program.cs` 加入三段，順序很重要。

**靜態檔服務**要排在 API 端點之前：

```csharp
// 前後端整合為單一站台後，以 HTTP 對外提供服務；啟用 HTTPS 重導向會讓沒有憑證的
// 部署環境無法載入前端靜態檔，因此僅在明確設定 UseHttpsRedirection=true 時才啟用。
if (builder.Configuration.GetValue("UseHttpsRedirection", false))
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();
```

`UseHttpsRedirection` 這段常被忽略：本機有 dev 憑證所以沒事，但部署到沒有憑證的環境時會讓前端整個載不進來。改成可設定的開關比較安全。

**移除佔用根路徑的端點。** 很多專案有 `app.MapGet("/", () => "API is running!")`，它會蓋掉 `index.html`。把存活檢查搬到 `/api/system/health`：

```csharp
// 根路徑改由 UseDefaultFiles/UseStaticFiles 提供前端 index.html；
// 後端存活檢查移至 /api/system/health。
app.MapGet("/api/system/health", () => Results.Ok(new { status = "ok" }));
```

**SPA fallback 放在所有端點之後**。前端用 history 模式時，直接重整 `/projects/1` 這類深層路由會 404，因為伺服器上沒有那個檔案：

```csharp
// SPA fallback：前端使用 createWebHistory()，直接重整 /projects/1 這類深層路由時
// 伺服器需回傳 index.html 交由前端路由接手。必須註冊在所有 API 端點之後，
// 且排除 /api 與 /ws，避免未命中的 API 請求被回傳 HTML 而非 404。
app.MapFallback((HttpContext context) =>
{
    var path = context.Request.Path;
    if (path.StartsWithSegments("/api") || path.StartsWithSegments("/ws"))
    {
        return Results.NotFound();
    }

    // 開發時若未建置前端，wwwroot/index.html 不存在（WebRootPath 亦可能為 null），
    // 此時回傳提示而非拋出例外——開發模式請改用 pnpm dev。
    var webRoot = app.Environment.WebRootPath;
    var indexPath = string.IsNullOrEmpty(webRoot) ? null : Path.Combine(webRoot, "index.html");

    if (indexPath is null || !File.Exists(indexPath))
    {
        return Results.NotFound("前端尚未建置。開發請執行前端的 dev server，部署請執行 dotnet publish。");
    }

    return Results.File(indexPath, "text/html");
});
```

排除 `/api` 這點很關鍵：少了它，打錯的 API 網址會回傳 HTML 而不是 404，前端拿到一坨 HTML 去 parse JSON 會噴出很難追的錯誤。`WebRootPath` 的 null 檢查也不是多餘的——`wwwroot` 不存在時它會是 null，直接 `Path.Combine` 會拋例外。

## 步驟四：publish 時自動建置前端

在 WebApi 的 `.csproj` 加入 MSBuild Target，讓 `dotnet publish` 一次完成前後端建置。掛在 `PrepareForPublish` 而非 `Build`，這樣日常 `dotnet build` 與跑測試都不會被前端建置拖慢：

```xml
<PropertyGroup>
  <SpaRoot>..\..\..\frontend\</SpaRoot>
  <PnpmCommand Condition="'$(OS)' == 'Windows_NT'">pnpm.cmd</PnpmCommand>
  <PnpmCommand Condition="'$(OS)' != 'Windows_NT'">pnpm</PnpmCommand>
</PropertyGroup>

<Target Name="BuildSpa" BeforeTargets="PrepareForPublish">
  <!-- 缺少 pnpm 時直接中止並給出明確指示，避免落入難以判讀的 exec 失敗訊息。 -->
  <Exec Command="$(PnpmCommand) --version" WorkingDirectory="$(SpaRoot)"
        ContinueOnError="true" StandardOutputImportance="low" StandardErrorImportance="low">
    <Output TaskParameter="ExitCode" PropertyName="PnpmCheckExitCode" />
  </Exec>
  <Error Condition="'$(PnpmCheckExitCode)' != '0'"
         Text="找不到 pnpm，無法建置前端。請先安裝：npm install -g pnpm" />

  <Message Importance="high" Text="正在建置前端 (pnpm build)..." />
  <Exec Command="$(PnpmCommand) install --frozen-lockfile" WorkingDirectory="$(SpaRoot)" />
  <Exec Command="$(PnpmCommand) build" WorkingDirectory="$(SpaRoot)" />
</Target>

<!--
  將前端產物納入 publish 輸出的 wwwroot。因為 dist/ 在 BuildSpa 執行後才存在，
  必須用動態 ItemGroup（Target 內）而非靜態 ItemGroup，否則評估階段會抓不到檔案。
-->
<Target Name="CopySpaToWwwroot" AfterTargets="BuildSpa" BeforeTargets="PrepareForPublish">
  <ItemGroup>
    <SpaDistFiles Include="$(SpaRoot)dist\**\*" />
    <ResolvedFileToPublish Include="@(SpaDistFiles)"
                           RelativePath="wwwroot\%(RecursiveDir)%(Filename)%(Extension)"
                           CopyToPublishDirectory="PreserveNewest" />
  </ItemGroup>
</Target>
```

動態 ItemGroup 那點是實作上的硬性限制：MSBuild 在評估階段就展開靜態 ItemGroup，那時 `dist/` 還不存在，所以檔案清單會是空的。放進 Target 內才會在執行時才展開。

若專案用 npm/yarn 而非 pnpm，替換 `PnpmCommand` 即可；套件管理器應該跟著專案既有慣例走，不要擅自更換。

## 步驟五：排除不該進 publish 的本機檔案

預設情況下，本機 SQLite 資料庫與開發用設定檔會被一起打包，部署時覆蓋線上資料或洩漏金鑰：

```xml
<ItemGroup>
  <Content Update="*.db" CopyToPublishDirectory="Never" />
  <Content Update="*.db-shm" CopyToPublishDirectory="Never" />
  <Content Update="*.db-wal" CopyToPublishDirectory="Never" />
</ItemGroup>
```

`appsettings.Development.json` 要不要排除，取決於使用者打算怎麼設定線上環境——這會影響部署後 AI 金鑰等設定能否讀到，**應該問使用者**，不要自己決定：

- **排除**：線上改用環境變數（`AI__ApiKey` 這種雙底線寫法）。金鑰不進部署包，較安全。
- **不排除**：線上設 `ASPNETCORE_ENVIRONMENT=Development` 直接沿用該檔。方便，但金鑰會進部署包，且該環境下的偵錯端點、測試後門也會一併生效。

若該檔含有測試用的驗證繞過開關（如 `Testing:EnableTestUserHeader`），選「不排除」時要提醒使用者：那個後門會在公開網址上生效。

## 步驟六：驗證

整合的問題幾乎都只在建置後才浮現，所以一定要實際 publish 並啟動來測，不能只看程式碼：

```bash
dotnet publish <WebApi 專案路徑> -c Release -o /tmp/publish-test
cd /tmp/publish-test && ASPNETCORE_URLS=http://localhost:5299 ./<專案名稱>
```

逐項確認（用一個沒被佔用的埠，避免與開發中的後端相撞）：

| 路徑 | 預期 |
|---|---|
| `/` | 200 text/html |
| 深層路由如 `/projects/1` | 200 text/html（fallback 生效） |
| `/api/<真實端點>` | 200 application/json |
| `/api/nonexistent` | 404，**不是** HTML |
| `/assets/*.js` | 200 text/javascript |
| WebSocket 端點 | 連線成功 |

WebSocket 可以用 node 快速驗證：

```bash
node -e "const WebSocket=require('ws');const ws=new WebSocket('ws://localhost:5299/ws');
ws.on('open',()=>{console.log('WS OK');process.exit(0)});
ws.on('error',e=>{console.log('WS FAIL:',e.message);process.exit(1)});"
```

最後跑一次既有測試，確認移除根路徑端點沒有打破什麼。

## 收尾

把建置產物加進 `.gitignore`（`publish` 輸出目錄、以及若有把前端建到專案 `wwwroot` 的話那個目錄）。

告訴使用者部署與開發各自怎麼跑：部署是單一 `dotnet publish`；開發仍維持前端 dev server + 後端分開跑，保有 HMR。
