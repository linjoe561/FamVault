# FamVault

管理家裡購買物品、庫存數量與保存期限的系統，家庭成員各自登入使用。

- 後端：ASP.NET Core Web API（.NET 10）＋ EF Core ＋ SQL Server
- 前端：Vue 3 ＋ Vite（手機優先的響應式版面）

## 快速開始

```bash
dotnet run --project backend/src/FamVault.Api
```

```bash
npm --prefix frontend install
npm --prefix frontend run dev
```

打開 http://localhost:5173 ，第一次會進入「第一次使用」畫面建立管理者。

## 文件

- [開發說明手冊](docs/開發說明手冊.md)：環境、架構、API、資料庫、設定
- [使用者說明手冊](docs/使用者說明手冊.md)：給家人看的操作說明
- [開發 skill](.claude/skills/famvault-dev/SKILL.md)：給 Claude Code 的開發規範
