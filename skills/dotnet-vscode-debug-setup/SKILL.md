---
name: dotnet-vscode-debug-setup
description: 建立 ASP.NET Core + 前端 SPA 專案的 VS Code 偵錯設定（launch.json 與 tasks.json），提供「單站台模式」與「前後端同時啟動」兩個選項，後端 C# 可下中斷點並在啟動後自動開瀏覽器。當使用者提到「設定 VS Code debug」「launch.json」「tasks.json」「按 F5 啟動」「要能下中斷點」「一鍵啟動前後端」，或抱怨中斷點沒作用、F5 啟動失敗、偵錯清單選項太多或名稱看不懂、啟動後沒有自動開瀏覽器、偵錯讀不到 appsettings.Development.json 時，務必使用此技能。使用者若只說「幫我設定偵錯」或「弄一個 F5 就能跑的設定」也應該觸發。
---

# .NET + 前端 SPA 的 VS Code 偵錯設定

建立兩種啟動方式：單獨啟動後端（單一站台，前端已建置進 wwwroot），以及前後端同時啟動（前端走 dev server 保有 HMR）。兩種模式下後端 C# 都能下中斷點。

## 核心觀念：中斷點靠的是 coreclr，不是 shell task

最常見的錯誤，是用 `tasks.json` 裡的 shell task 跑 `dotnet run` 來啟動後端，再用 `chrome` 型別的 launch 設定開瀏覽器。這樣前端 TypeScript 能下中斷點，但**後端 C# 不行**——因為 .NET 偵錯器根本沒附加上去。

要讓 C# 能下中斷點，後端必須由 `"type": "coreclr"` 的 launch 設定啟動，指向 Debug 建置的 dll。Release 建置的最佳化會讓中斷點錯位或失效，所以務必用 Debug。

前後端要一起跑，就用 `compounds` 把後端的 coreclr 設定與前端的 chrome 設定組合起來，而不是把後端塞進 preLaunchTask。

## 步驟一：偵查專案結構

```bash
# WebApi 專案路徑與組件名稱（決定 dll 路徑）
find . -name "*.csproj" -path "*WebApi*" -not -path "*/obj/*"

# TargetFramework（決定 bin/Debug/<tfm>/ 的 tfm 值）
grep -h "TargetFramework" **/Directory.Build.props **/*WebApi*.csproj 2>/dev/null

# 後端埠號
cat <WebApi 專案>/Properties/launchSettings.json

# 前端 dev server 埠號與套件管理器
cat frontend/vite.config.ts; ls frontend/pnpm-lock.yaml frontend/package-lock.json frontend/yarn.lock 2>/dev/null
```

dll 路徑是 `<專案目錄>/bin/Debug/<TargetFramework>/<組件名>.dll`，猜錯的話 F5 會直接失敗，所以務必用實際查到的值。

## 步驟二：tasks.json

三個 task。注意 `build` 用 `"type": "process"`（VS Code 對 .NET 的標準寫法），前端相關的用 `"type": "shell"`。

```jsonc
{
  "version": "2.0.0",
  "tasks": [
    {
      // .NET 標準建置 task，供 launch.json 的 coreclr 偵錯設定作為 preLaunchTask。
      // 先建前端到 wwwroot，讓偵錯啟動的後端埠也能直接開出前端畫面
      // （dotnet build 不會觸發 csproj 的 publish-only Target）。
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/<WebApi 專案路徑>.csproj",
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary"
      ],
      "dependsOn": ["build-frontend-to-wwwroot"],
      "problemMatcher": "$msCompile"
    },
    {
      // 將前端建置到後端專案的 wwwroot，讓偵錯啟動時後端埠也能開啟前端畫面。
      // 用 --outDir 覆寫輸出位置，不動 vite.config.ts 的預設 dist/（publish 流程依賴它）。
      // --emptyOutDir 讓 Vite 願意寫入專案外的目錄並清掉舊產物。
      "label": "build-frontend-to-wwwroot",
      "type": "shell",
      "command": "pnpm exec vite build --outDir ../<WebApi 專案目錄>/wwwroot --emptyOutDir",
      "options": { "cwd": "${workspaceFolder}/frontend" },
      "problemMatcher": [],
      "presentation": { "reveal": "always", "panel": "shared", "group": "servers" }
    },
    {
      // 前端 dev server；後端由 coreclr 偵錯設定啟動，故不需要 start-backend task。
      "label": "start-frontend",
      "type": "shell",
      "command": "pnpm --dir frontend dev",
      "isBackground": true,
      "problemMatcher": {
        "owner": "custom",
        "pattern": { "regexp": "^$" },
        "background": {
          "activeOnStart": true,
          "beginsPattern": "^.*$",
          "endsPattern": "Local:\\s+http"
        }
      },
      "presentation": { "reveal": "always", "panel": "shared", "group": "servers" }
    }
  ]
}
```

**前端建置不要用 `pnpm build`。** 多數 Vite 專案的 `package.json` 裡 build 是 `vue-tsc -b && vite build`（或 `tsc && vite build`），附加的 `--outDir` 參數會被前段的型別檢查工具接走而完全失效——建置會成功，但檔案還是輸出到 `dist/`，看起來像設定沒生效卻找不到原因。直接呼叫 `vite` 可以避開這點，代價是跳過型別檢查（偵錯情境下反而啟動更快，正式 publish 仍走完整流程）。

`isBackground` 的 `endsPattern` 要對得上 dev server 的實際輸出，否則 VS Code 會一直等下去。

## 步驟三：launch.json

```jsonc
{
  "version": "0.2.0",
  "configurations": [
    {
      // 單站台模式：前端已由 build task 建置進 wwwroot，前後端同源，等同正式部署樣貌。
      // 以 Debug 建置啟動，可於 C# 程式碼下中斷點；
      // Development 環境會載入 appsettings.Development.json。
      "name": "🌐 單站台模式 (前後端同源)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/<WebApi 專案目錄>/bin/Debug/<tfm>/<組件名>.dll",
      "args": [],
      "cwd": "${workspaceFolder}/<WebApi 專案目錄>",
      "stopAtEntry": false,
      // 啟動後自動開啟瀏覽器（pattern 的選擇見下方說明）。
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
        "uriFormat": "%s"
      },
      "env": { "ASPNETCORE_ENVIRONMENT": "Development" }
    },
    {
      // 前端偵錯：附加到 Vite dev server，可於 Vue/TS 程式碼下中斷點。
      // 僅作為 compound 的組成零件，故以 presentation.hidden 隱藏，
      // 讓偵錯清單只出現使用者真正會選的兩個選項。
      "name": "前端偵錯 (Chrome)",
      "type": "chrome",
      "request": "launch",
      "url": "http://localhost:5173",
      "webRoot": "${workspaceFolder}/frontend",
      "preLaunchTask": "start-frontend",
      "sourceMaps": true,
      "presentation": { "hidden": true }
    }
  ],
  "compounds": [
    {
      // 同時啟動前後端：前端走 Vite dev server 保有 HMR。
      // 兩邊都掛上偵錯器，C# 與 Vue/TS 皆可下中斷點。
      "name": "⚡ 開發模式 (前端 HMR + 後端)",
      "configurations": ["🌐 單站台模式 (前後端同源)", "前端偵錯 (Chrome)"],
      "stopAll": true
    }
  ]
}
```

## 讓偵錯清單只出現該出現的選項

VS Code 會把每個 configuration 與 compound 都列進啟動清單，所以上面這份設定預設會顯示三個選項——但「前端偵錯」只是 compound 的組成零件，單獨啟動沒有意義（後端沒起來）。

用 `"presentation": { "hidden": true }` 把它從清單移除，它仍然照常作為 compound 的一部分運作。使用者看到的就只剩兩個有意義的選項。

命名也值得花點心思：`.NET 後端偵錯 (WebApi)` 這種以技術命名的方式，使用者得自己推敲差別在哪。改成以**使用情境**命名更直覺，例如「單站台模式 (前後端同源)」與「開發模式 (前端 HMR + 後端)」，一眼就知道該選哪個。

## serverReadyAction：注意 log 等級可能吃掉比對訊息

`serverReadyAction` 讓後端啟動後自動開瀏覽器，靠的是**比對主控台輸出**。預設寫法比對 Kestrel 的 `Now listening on:`：

```jsonc
"serverReadyAction": {
  "action": "openExternally",
  "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
  "uriFormat": "%s"
}
```

**但這個訊息在很多專案裡根本不會出現。** 它是 `Microsoft.Hosting.Lifetime` 發出的 Information 等級訊息，而 Serilog／預設 logging 設定常見這樣的覆寫：

```json
"MinimumLevel": { "Override": { "Microsoft": "Warning" } }
```

一旦 `Microsoft` 被壓到 Warning，該訊息就被濾掉，pattern 永遠不會命中——瀏覽器靜默地不開，而且看不出任何錯誤，很容易誤以為是 `serverReadyAction` 不支援。

所以設定前先確認訊息是否真的存在：

```bash
grep -n "MinimumLevel" -A5 <WebApi 專案目錄>/appsettings.json
```

若 `Microsoft` 被覆寫為 Warning 以上，改比對一個該專案確定會輸出的啟動訊息（例如自訂 HostedService 的啟動 log），並用 `uriFormat` 直接指定固定網址：

```jsonc
"serverReadyAction": {
  "action": "openExternally",
  "pattern": "AI Job Processor is starting",
  "uriFormat": "http://localhost:5082"
}
```

用 HostedService 的訊息要留意時序：它可能在 Kestrel 綁定埠之前就發出，瀏覽器開太早會連線被拒。實測確認即可——啟動後端並在偵測到該訊息時立刻打 health 端點，看是否已可連線。多數情況下兩者幾乎同時，不成問題。

**`cwd` 必須指向專案目錄**，不能指向 publish 輸出目錄。ASP.NET Core 從執行目錄尋找 `appsettings.*.json`，而 `appsettings.Development.json` 通常只存在於專案目錄（publish 時常被刻意排除）。指錯的話設定會落回 `appsettings.json` 的預設值，症狀是連到錯誤的服務位址、一直重試失敗，但看起來又不像設定問題。

`stopAll: true` 讓停止其中一個偵錯工作階段時另一個也一起停，避免殘留程序佔著埠。

## 步驟四：收尾與驗證

把前端建置產物加進 `.gitignore`：

```
<WebApi 專案目錄>/wwwroot/
```

不需要 `.gitkeep`——`build` task 會透過 `dependsOn` 自動建立該目錄。

驗證方式（模擬偵錯器的實際啟動方式，用一個沒被佔用的埠）：

```bash
dotnet build <WebApi 專案>.csproj
cd <WebApi 專案目錄>
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5299 \
  dotnet bin/Debug/<tfm>/<組件名>.dll
```

確認：前端畫面能開（`/` 回 200 text/html）、深層路由能開、以及讀得到 `appsettings.Development.json` 裡的設定（挑一個會反映該設定的端點打打看）。

## 埠號衝突：最常見的 F5 失敗原因

`Failed to bind to address ... address already in use` 幾乎都是舊程序沒關，而不是設定壞掉。這在切換啟動方式時特別容易發生（例如先前用 `dotnet run` 或跑過 publish 版站台，那些程序不會因為改了設定就消失）。

```bash
lsof -nP -iTCP:<埠號> -sTCP:LISTEN    # 找出佔用者
ps -p <PID> -o pid,lstart,command      # 確認身分再動手，避免關錯
kill <PID>
```

先確認再關。使用者機器上可能同時跑著其他專案的服務。
