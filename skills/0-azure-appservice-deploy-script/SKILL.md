---
name: 0-azure-appservice-deploy-script
description: 建立一鍵部署 .NET 專案到 Azure App Service 的 bash 腳本，透過 Azure Portal 下載的發行設定檔（.publishsettings）解析認證，用 Kudu zipdeploy API 上傳。當使用者提到「deploy-azure.sh」「部署到 Azure」「App Service 部署腳本」「publish profile 部署」「一鍵部署」「zipdeploy」，或想要不裝 Azure CLI 就能部署時，務必使用此技能。使用者若只說「加一個部署腳本」或「幫我弄個上 Azure 的 sh」也應該觸發。
---

# Azure App Service 一鍵部署腳本

產生 `deploy-azure.sh`：從 `.publishsettings` 解析認證 → `dotnet publish` → 打包 zip → 經 Kudu `zipdeploy` API 上傳。不需要安裝 Azure CLI 或登入，適合本機或 CI 直接跑。

## 使用方式

`assets/deploy-azure.sh.template` 是完整腳本，含三個佔位符。讀取該檔、替換佔位符、寫入專案根目錄的 `deploy-azure.sh`，然後 `chmod +x`。

| 佔位符 | 替換為 |
|---|---|
| `{{PROJECT_PATH}}` | WebApi 的 .csproj 相對路徑，如 `backend/src/Acme.WebApi/Acme.WebApi.csproj` |
| `{{APP_DISPLAY_NAME}}` | 顯示在標題橫幅的專案名稱 |
| `{{REMINDERS}}` | 結尾提醒（見下方） |

先找出實際的專案路徑，不要用猜的：

```bash
find . -name "*.csproj" -path "*WebApi*" -not -path "*/obj/*"
```

## `{{REMINDERS}}` 要寫什麼

這段是部署完成後印給使用者看的提醒，內容取決於專案的實際設定，**應該根據專案狀況調整，必要時問使用者**。每行格式是 `echo "   - ..."`。

判斷依據：

- **資料庫**：若 `.csproj` 有排除 `*.db`（SQLite 不隨部署上傳），提醒 App Service 首次啟動會自動建立。若用 SQL Server，提醒確認連線字串已設定。
- **設定檔**：若 `appsettings.Development.json` 被排除出 publish，列出線上需要設定的環境變數。ASP.NET Core 的巢狀 key 在環境變數中以**雙底線**分隔，例如 `AI:ApiKey` 要寫成 `AI__ApiKey`——這點很容易寫錯成單底線或冒號。若該檔沒被排除，改為提醒需設定 `ASPNETCORE_ENVIRONMENT=Development` 才會載入它。

範例（SQLite + 環境變數設定的情況）：

```bash
echo "   - 本專案使用 SQLite，資料庫不隨部署上傳，App Service 首次啟動會自動建立。"
echo "   - appsettings.Development.json 不會被打包上傳，請於 App Service 的"
echo "     「環境變數 / 應用程式設定」設定以下項目（巢狀 key 以雙底線分隔）："
echo "       AI__Endpoint       例: https://<資源名>.openai.azure.com/openai/v1/"
echo "       AI__ApiKey         金鑰"
```

## 腳本的設計重點

改動腳本時留意這幾處，它們是刻意這樣寫的：

**`set -euo pipefail`** — 比單純 `set -e` 更嚴格，未定義變數與 pipe 中段失敗都會中止。部署腳本中途失敗卻繼續跑下去是最糟的情況。

**`cd "$(dirname "$0")"`** — 以腳本所在目錄為工作根目錄，從任何路徑呼叫都能正確解析相對路徑。

**`curl --fail-with-body`** — 少了這個，HTTP 錯誤（401 認證失敗、413 檔案過大）時 curl 仍回傳 0，腳本會印出「部署成功」但其實什麼都沒上去。這是很難察覺的假成功。

**publish 後檢查 `wwwroot/index.html`** — 若專案是前後端整合的單一站台，前端建置失敗時會部署出一個空站台。提前中止比部署後才發現好。若專案是純 API 沒有前端，移除這段檢查。

**`zip -r -q`** — `-q` 避免上千個檔名洗版蓋掉真正重要的訊息。

**`publishUrl` 去掉 `:443`** — 設定檔裡的值帶著埠號，直接組成 URL 會變成 `https://host:443/api/zipdeploy`，多數情況可用但偶爾會出問題，去掉較穩妥。

**站台網址取自 `destinationAppUrl`** — 部署完成後印出網址，使用者可直接點開驗證。注意這個屬性不是每份設定檔都有（視 Azure 產生的版本而定），所以缺少時以 `profileName` 推導 `https://<app>.azurewebsites.net` 作為降級。別把 `publishUrl` 拿來當站台網址——那是 Kudu 管理端點（`<app>.scm.azurewebsites.net`），不是站台本身。

## 收尾

`.publishsettings` 含 App Service 的部署密碼，**絕對不能入版控**。加入 `.gitignore`：

```
publish_profiles/
*.publishsettings
publish_output/
deploy.zip
```

告訴使用者取得設定檔的方式：Azure Portal → App Service 頁面頂端 →「下載發行設定檔」(Get publish profile) → 放進專案根目錄的 `publish_profiles/`。

腳本會自動建立 `publish_profiles/` 目錄，也會降級檢查專案根目錄，所以放哪都能跑。

## 驗證

`bash -n deploy-azure.sh` 檢查語法。不帶設定檔跑一次，應該在第一步就擋下並印出取得設定檔的指示——這確認防呆有效。

完整的部署流程不要拿正式環境試。若使用者已在其他專案驗證過這個腳本，就不需要再測部署本身。
