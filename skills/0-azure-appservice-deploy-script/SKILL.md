---
name: 0-azure-appservice-deploy-script
description: Create a one-click bash script that deploys a .NET project to Azure App Service — it parses credentials from the publish profile (.publishsettings) downloaded from the Azure Portal and uploads through the Kudu zipdeploy API. Use it whenever the user mentions deploy-azure.sh, deploying to Azure, an App Service deploy script, publish-profile deployment, one-click deploy or zipdeploy (「部署到 Azure」「App Service 部署腳本」「一鍵部署」「加一個部署腳本」「弄個上 Azure 的 sh」), or wants to deploy without installing the Azure CLI.
---

# Azure App Service One-Click Deploy Script

Generate `deploy-azure.sh`: parse credentials from `.publishsettings` → `dotnet publish` → zip → upload through the Kudu `zipdeploy` API. No Azure CLI install or login is needed, so it runs locally or in CI as is.

The generated script's comments and messages are in Traditional Chinese (正體中文), as in the template.

## Usage

`assets/deploy-azure.sh.template` is the complete script with three placeholders. Read it, replace the placeholders, write it to `deploy-azure.sh` in the project root, then `chmod +x`.

| Placeholder | Replace with |
|---|---|
| `{{PROJECT_PATH}}` | Relative path of the WebApi `.csproj`, e.g. `backend/src/Acme.WebApi/Acme.WebApi.csproj` |
| `{{APP_DISPLAY_NAME}}` | Project name shown in the title banner |
| `{{REMINDERS}}` | Closing reminders (see below) |

Find the real project path rather than guessing:

```bash
find . -name "*.csproj" -path "*WebApi*" -not -path "*/obj/*"
```

## What goes into `{{REMINDERS}}`

These are the reminders printed to the user after deployment. Their content depends on the project's actual setup — **tailor them to the project and ask the user when unsure**. Each line has the form `echo "   - ..."`.

Decide from:

- **Database**: if the `.csproj` excludes `*.db` (SQLite is not uploaded), remind that App Service creates it on first start. With SQL Server, remind to confirm the connection string is configured.
- **Settings**: if `appsettings.Development.json` is excluded from publish, list the environment variables the live site needs. ASP.NET Core nested keys use a **double underscore** in environment variables — `AI:ApiKey` becomes `AI__ApiKey`; a single underscore or a colon is the common mistake. If the file is not excluded, remind instead that `ASPNETCORE_ENVIRONMENT=Development` is required for it to load.

Example (SQLite + environment-variable settings):

```bash
echo "   - 本專案使用 SQLite，資料庫不隨部署上傳，App Service 首次啟動會自動建立。"
echo "   - appsettings.Development.json 不會被打包上傳，請於 App Service 的"
echo "     「環境變數 / 應用程式設定」設定以下項目（巢狀 key 以雙底線分隔）："
echo "       AI__Endpoint       例: https://<資源名>.openai.azure.com/openai/v1/"
echo "       AI__ApiKey         金鑰"
```

## Deliberate design points

Keep these intact when editing the script:

**`set -euo pipefail`** — stricter than `set -e`: undefined variables and failures mid-pipe abort too. A deploy script that keeps going after a failure is the worst outcome.

**`cd "$(dirname "$0")"`** — the script's own directory is the working root, so relative paths resolve from wherever it is called.

**`curl --fail-with-body`** — without it, curl returns 0 on HTTP errors (401 bad credentials, 413 payload too large) and the script prints "deploy succeeded" while nothing was uploaded — a false success that is hard to notice.

**Check `wwwroot/index.html` after publish** — in a single-site frontend + backend project, a failed frontend build deploys an empty site. Aborting early beats discovering it after deployment. Remove the check for a pure API with no frontend.

**`zip -r -q`** — `-q` keeps thousands of file names from burying the messages that matter.

**Strip `:443` from `publishUrl`** — the profile value carries the port; building `https://host:443/api/zipdeploy` from it mostly works but occasionally fails, so strip it.

**Site URL from `destinationAppUrl`** — printed after deployment so the user can open it to verify. Not every profile has this attribute (it depends on the version Azure generated), so fall back to `https://<app>.azurewebsites.net` derived from `profileName`. `publishUrl` is the Kudu management endpoint (`<app>.scm.azurewebsites.net`), not the site.

## Wrap-up

`.publishsettings` holds the App Service deployment password and must stay out of version control. Add to `.gitignore`:

```
publish_profiles/
*.publishsettings
publish_output/
deploy.zip
```

Tell the user how to get the profile: Azure Portal → App Service page top bar → "Get publish profile"（下載發行設定檔）→ place it in `publish_profiles/` under the project root.

The script creates `publish_profiles/` itself and falls back to the project root, so either location works.

## Verification

`bash -n deploy-azure.sh` checks syntax. Run it once without a profile: it should stop at the first step and print how to get the profile — this confirms the guard works.

Never test a full deployment against production. If the user has already validated this script on another project, the deployment itself needs no retest.
