---
name: 0-spa-backend-single-site
description: Merge a standalone frontend SPA (Vue/React/Vite) and an ASP.NET Core backend into a single-site deployment, so dotnet publish builds the frontend into wwwroot automatically. Use it whenever the user mentions integrating frontend and backend, deploying only one site, merging into a single site, putting the frontend into wwwroot, not deploying them separately, a single App Service (「前後端整合」「只想部署一個站台」「合併成單一站台」「前端放進 wwwroot」「整合前端」「一個站台就好」), or asks how the backend can serve both the API and the frontend static files.
---

# Merge Frontend and Backend into a Single Site

Combine an SPA (Vite dev server) and an ASP.NET Core API that used to run separately into one site: the backend serves both the API and the frontend static files, so deployment needs only one App Service / container.

Comments and messages in the code this skill adds to the project are in Traditional Chinese (正體中文), as in the snippets below.

## Why this is easy to get wrong

Integration looks like "copy dist into wwwroot", but four things blow up only after deployment, and local development hides all of them. Scan for these four before changing anything.

## Step 1: Inspect before changing

Confirm the following before integrating; they decide which files change:

```bash
# Backend addresses hard-coded in the frontend (the most common trap)
grep -rn "localhost:[0-9]\+\|ws://\|wss://\|baseURL\|VITE_" frontend/src/

# Router mode: createWebHistory needs an SPA fallback, createWebHashHistory does not
grep -rn "createWebHistory\|createWebHashHistory\|BrowserRouter\|HashRouter" frontend/src/

# Whether the backend already has an endpoint on the root path
grep -n 'MapGet("/"' backend/**/Program.cs
```

Also read `vite.config.ts` (existing proxy settings) and `Program.cs` (middleware order).

## Step 2: Make frontend connections same-origin

**WebSocket is the item most often missed.** API calls usually already use relative paths (via the Vite proxy), but WebSocket is often hard-coded as `ws://localhost:5082`. After integration frontend and backend share an origin, so use `window.location.host`:

```ts
const wsUrl = computed(() => {
  const loc = window.location
  const proto = loc.protocol === 'https:' ? 'wss:' : 'ws:'
  // 同源連線：正式部署時前後端同一站台，開發時由 Vite 的 /ws proxy 轉發到後端。
  return `${proto}//${loc.host}/ws`
})
```

Once same-origin, development mode (Vite :5173) needs a proxy to reach the backend, so add `/ws` to `vite.config.ts` (`ws: true` is required, or the connection is never upgraded to WebSocket):

```ts
server: {
  proxy: {
    '/api': { target: 'http://localhost:5082', changeOrigin: true, secure: false },
    // WebSocket 已改為同源連線，開發模式需由 dev server 轉發到後端。
    '/ws': { target: 'ws://localhost:5082', ws: true, changeOrigin: true, secure: false }
  }
}
```

## Step 3: Serve static files and an SPA fallback from the backend

Add three parts to `Program.cs`; order matters.

**Static file serving** goes before the API endpoints:

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

The `UseHttpsRedirection` part is often overlooked: locally the dev certificate hides the problem, but in a deployment without a certificate the frontend fails to load entirely. A configurable switch is safer.

**Remove any endpoint on the root path.** Many projects have `app.MapGet("/", () => "API is running!")`, which shadows `index.html`. Move the liveness check to `/api/system/health`:

```csharp
// 根路徑改由 UseDefaultFiles/UseStaticFiles 提供前端 index.html；
// 後端存活檢查移至 /api/system/health。
app.MapGet("/api/system/health", () => Results.Ok(new { status = "ok" }));
```

**The SPA fallback goes after all endpoints.** With history-mode routing, reloading a deep route like `/projects/1` returns 404 because no such file exists on the server:

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

Excluding `/api` is essential: without it, a mistyped API URL returns HTML instead of 404, and the frontend fails parsing that HTML as JSON with an error that is hard to trace. The `WebRootPath` null check is not redundant either — it is null when `wwwroot` does not exist, and `Path.Combine` would throw.

## Step 4: Build the frontend automatically on publish

Add MSBuild targets to the WebApi `.csproj` so `dotnet publish` builds frontend and backend in one go. Hook into `PrepareForPublish` rather than `Build`, so everyday `dotnet build` and test runs are not slowed by the frontend build:

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

The dynamic ItemGroup is a hard constraint: MSBuild expands static ItemGroups at evaluation time, when `dist/` does not exist yet, so the file list would be empty. Inside a Target it expands at execution time.

If the project uses npm/yarn instead of pnpm, replace `PnpmCommand`; the package manager follows the project's existing convention and is never switched unasked.

## Step 5: Keep local-only files out of publish

Nothing that exists only for local development or testing is published: it overwrites live data, leaks keys, or ships noise. First find what this project has:

```bash
# 本機資料庫、log、開發用設定檔與其他執行時產生的檔案
find <WebApi project dir> \( -name "*.db*" -o -name "*.log" -o -name "appsettings.*.json" \) -not -path "*/bin/*" -not -path "*/obj/*"
# log 與本機儲存的輸出目錄（Serilog 的 path、LocalFileStorage 的根目錄）
grep -n "path\|Path\|Directory" <WebApi project dir>/appsettings*.json
```

Then exclude every one of them in the WebApi `.csproj`. The block below covers the usual set; add the project's own directories (local upload storage, test fixtures) to both lines:

```xml
<!-- 僅供本機開發／測試使用的檔案一律不隨 publish 輸出：開發設定、本機資料庫、log。 -->
<ItemGroup>
  <Content Update="appsettings.Development.json;logs\**;*.db;*.db-shm;*.db-wal;*.log" CopyToPublishDirectory="Never" />
  <None Update="logs\**;*.db;*.db-shm;*.db-wal;*.log" CopyToPublishDirectory="Never" />
</ItemGroup>
```

Both lines are needed: the Web SDK treats `*.json` (including JSON-formatted logs) as `Content`, and files the project marks `CopyToOutputDirectory` are usually `None`. `Update` only touches items that exist, so a pattern with no match is harmless.

`appsettings.Development.json` is always excluded. Tell the user which settings the live site therefore needs as environment variables, in double-underscore form (`AI:ApiKey` → `AI__ApiKey`).

If the project has a test authentication bypass (e.g. `Testing:EnableTestUserHeader`), confirm it is enabled only by that excluded file and report it to the user.

## Step 6: Verify

Integration problems almost always surface only after a build, so actually publish and start it; reading the code is not enough:

```bash
dotnet publish <WebApi project path> -c Release -o /tmp/publish-test
cd /tmp/publish-test && ASPNETCORE_URLS=http://localhost:5299 ./<project name>
```

Check each item (on a free port, so it does not collide with a running dev backend):

| Path | Expected |
|---|---|
| `/` | 200 text/html |
| Deep route such as `/projects/1` | 200 text/html (fallback works) |
| `/api/<real endpoint>` | 200 application/json |
| `/api/nonexistent` | 404, **not** HTML |
| `/assets/*.js` | 200 text/javascript |
| WebSocket endpoint | Connects |

Also list the publish output and confirm none of the Step 5 files are in it (`find /tmp/publish-test -name "*.db*" -o -name "*.log" -o -name "appsettings.Development.json"` prints nothing).

WebSocket can be checked quickly with node:

```bash
node -e "const WebSocket=require('ws');const ws=new WebSocket('ws://localhost:5299/ws');
ws.on('open',()=>{console.log('WS OK');process.exit(0)});
ws.on('error',e=>{console.log('WS FAIL:',e.message);process.exit(1)});"
```

Finally run the existing tests to confirm removing the root endpoint broke nothing.

## Wrap-up

Add build output to `.gitignore` (the `publish` output directory, and the project's `wwwroot` if the frontend is built there).

Tell the user how deployment and development each run: deployment is a single `dotnet publish`; development keeps the frontend dev server and backend running separately, with HMR.
