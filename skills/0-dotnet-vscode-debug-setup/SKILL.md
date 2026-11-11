---
name: 0-dotnet-vscode-debug-setup
description: Create VS Code debug settings (launch.json and tasks.json) for an ASP.NET Core + frontend SPA project, with two options — single-site mode and frontend + backend together — where backend C# breakpoints work and a browser opens after startup. Use it whenever the user mentions setting up VS Code debugging, launch.json, tasks.json, F5 startup, breakpoints, or starting frontend and backend together (「設定 VS Code debug」「按 F5 啟動」「要能下中斷點」「一鍵啟動前後端」「幫我設定偵錯」「弄一個 F5 就能跑的設定」), or complains that breakpoints do nothing, F5 fails, the debug list has too many or unclear entries, no browser opens after startup, or debugging cannot read appsettings.Development.json.
---

# VS Code Debug Settings for .NET + Frontend SPA

Create two launch modes: the backend alone (single site, frontend already built into wwwroot), and frontend + backend together (frontend on its dev server, keeping HMR). In both modes backend C# breakpoints work.

The generated `launch.json` / `tasks.json` comments and configuration names are in Traditional Chinese (正體中文), as in the templates below.

## Core idea: breakpoints come from coreclr, not a shell task

The most common mistake is starting the backend with a shell task in `tasks.json` that runs `dotnet run`, plus a `chrome` launch configuration for the browser. Frontend TypeScript breakpoints then work, but **backend C# breakpoints do not** — the .NET debugger was never attached.

For C# breakpoints, the backend must be started by a `"type": "coreclr"` launch configuration pointing at the Debug-build dll. Release-build optimizations shift or disable breakpoints, so always use Debug.

To run frontend and backend together, combine the backend coreclr configuration and the frontend chrome configuration with `compounds`, rather than pushing the backend into a preLaunchTask.

## Step 1: Inspect the project

```bash
# WebApi project path and assembly name (determine the dll path)
find . -name "*.csproj" -path "*WebApi*" -not -path "*/obj/*"

# TargetFramework (the <tfm> in bin/Debug/<tfm>/)
grep -h "TargetFramework" **/Directory.Build.props **/*WebApi*.csproj 2>/dev/null

# Backend port
cat <WebApi project>/Properties/launchSettings.json

# Frontend dev server port and package manager
cat frontend/vite.config.ts; ls frontend/pnpm-lock.yaml frontend/package-lock.json frontend/yarn.lock 2>/dev/null
```

The dll path is `<project dir>/bin/Debug/<TargetFramework>/<assembly name>.dll`. A wrong guess makes F5 fail immediately, so use the values you actually found.

## Step 2: tasks.json

Three tasks. `build` uses `"type": "process"` (VS Code's standard form for .NET); the frontend tasks use `"type": "shell"`.

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
        "${workspaceFolder}/<WebApi project path>.csproj",
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
      "command": "pnpm exec vite build --outDir ../<WebApi project dir>/wwwroot --emptyOutDir",
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

**Do not build the frontend with `pnpm build`.** In most Vite projects `package.json` defines build as `vue-tsc -b && vite build` (or `tsc && vite build`); an appended `--outDir` is swallowed by the type-check tool in front and has no effect — the build succeeds but still writes to `dist/`, which looks like the setting is ignored for no reason. Calling `vite` directly avoids this at the cost of skipping type checking (which makes debug startup faster; the real publish still runs the full pipeline).

The templates use pnpm. A project on npm or yarn is the exception: keep its package manager and swap the two commands (`npx vite build …`, `npm --prefix frontend run dev`).

The `endsPattern` of an `isBackground` task must match the dev server's real output, or VS Code waits forever.

## Step 3: launch.json

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
      "program": "${workspaceFolder}/<WebApi project dir>/bin/Debug/<tfm>/<assembly name>.dll",
      "args": [],
      "cwd": "${workspaceFolder}/<WebApi project dir>",
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

## Show only the entries that belong in the debug list

VS Code lists every configuration and compound in the launch list, so the settings above would show three entries by default — but the frontend-debug configuration is only a building block of the compound; starting it alone is pointless (no backend).

`"presentation": { "hidden": true }` removes it from the list while it still works inside the compound. The user sees only the two meaningful options.

Naming matters too: a technical name like `.NET 後端偵錯 (WebApi)` makes the user work out the difference. Name by **use case** instead, e.g. 「單站台模式 (前後端同源)」 and 「開發模式 (前端 HMR + 後端)」, so the right choice is obvious at a glance.

## serverReadyAction: the log level may swallow the matched message

`serverReadyAction` opens the browser after the backend starts by **matching console output**. The default pattern matches Kestrel's `Now listening on:`:

```jsonc
"serverReadyAction": {
  "action": "openExternally",
  "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
  "uriFormat": "%s"
}
```

**In many projects this message never appears.** It is an Information-level message from `Microsoft.Hosting.Lifetime`, and Serilog / default logging setups often override it like this:

```json
"MinimumLevel": { "Override": { "Microsoft": "Warning" } }
```

Once `Microsoft` is raised to Warning, the message is filtered out and the pattern never matches — the browser silently stays closed with no visible error, which is easy to mistake for `serverReadyAction` not being supported.

So confirm the message really exists before configuring it:

```bash
grep -n "MinimumLevel" -A5 <WebApi project dir>/appsettings.json
```

If `Microsoft` is overridden to Warning or higher, match a startup message the project is sure to log (e.g. a custom HostedService's startup log) and give a fixed URL in `uriFormat`:

```jsonc
"serverReadyAction": {
  "action": "openExternally",
  "pattern": "AI Job Processor is starting",
  "uriFormat": "http://localhost:5082"
}
```

Mind the timing with a HostedService message: it may be logged before Kestrel binds the port, so the browser could open too early and get connection refused. Verify by starting the backend and hitting a health endpoint the moment the message appears. In most cases the two happen almost together and it is not a problem.

**`cwd` must point at the project directory**, not the publish output. ASP.NET Core looks for `appsettings.*.json` in the working directory, and `appsettings.Development.json` usually exists only in the project directory (publish often excludes it on purpose). Pointing elsewhere falls back to `appsettings.json` defaults; the symptom is connecting to the wrong service address and retrying endlessly, which does not look like a settings problem.

`stopAll: true` stops the other debug session when one stops, so no leftover process holds the port.

## Step 4: Wrap-up and verification

Add the frontend build output to `.gitignore`:

```
<WebApi project dir>/wwwroot/
```

No `.gitkeep` is needed — the `build` task creates the directory through `dependsOn`.

Verify (mimicking how the debugger starts it, on a free port):

```bash
dotnet build <WebApi project>.csproj
cd <WebApi project dir>
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5299 \
  dotnet bin/Debug/<tfm>/<assembly name>.dll
```

Confirm: the frontend opens (`/` returns 200 text/html), deep routes open, and settings from `appsettings.Development.json` are read (hit an endpoint that reflects one of them).

## Port conflicts: the most common F5 failure

`Failed to bind to address ... address already in use` almost always means an old process is still running, not broken settings. It happens especially when switching launch styles (processes from an earlier `dotnet run` or a published site do not disappear because the settings changed).

```bash
lsof -nP -iTCP:<port> -sTCP:LISTEN    # find the owner
ps -p <PID> -o pid,lstart,command      # confirm what it is before killing
kill <PID>
```

Confirm before killing: the user's machine may be running services from other projects.
