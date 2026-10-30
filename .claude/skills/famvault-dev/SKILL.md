---
name: famvault-dev
description: FamVault 家庭庫存系統的開發規範與操作手冊。在這個專案新增或修改功能、建立後端模組或前端模組、新增資料表與 Migration、啟動或測試前後端、調整 UI 樣式時使用。
---

# FamVault 開發指引

家庭庫存管理系統：記錄家中物品的購買、庫存數量與保存期限，多位家庭成員各自登入。
完整說明見 `docs/開發說明手冊.md`；這份 skill 是動手前必須遵守的重點。

## 技術與位置

| 部分 | 技術 | 位置 |
|---|---|---|
| 後端 | .NET 10、ASP.NET Core Web API（Controllers）、EF Core 10、FluentValidation、ASP.NET Core Identity + JWT | `backend/` |
| 前端 | Vue 3（JavaScript、`<script setup>`）、Vite、Vue Router、Pinia、Axios、Reka UI（無樣式元件） | `frontend/` |
| 資料庫 | SQL Server（本機預設執行個體 `.`，資料庫 `FamVault`，Windows 驗證） | — |

前後端完全分離：前端只透過 `/api/v1/...` 的 REST JSON 與後端溝通。

## 架構原則（不可違反）

1. **模組化單體**：每個功能是一個模組，後端 `backend/src/Modules/<名稱>/`，前端 `frontend/src/modules/<名稱>/`。
2. **模組之間不直接參照**：後端模組只能參照 `FamVault.SharedKernel`。需要別的模組的資料時，在 `SharedKernel/Contracts/` 定義介面，由擁有資料的模組實作（範例：`IUserDirectory`）。
3. **每個模組有自己的 DbContext 與 schema**：用 `services.AddModuleDbContext<T>(configuration, schema)` 註冊。跨模組只存 Id，不建立跨 schema 外鍵。
4. **schema 名稱避開 T-SQL 保留字**（例如 `identity`、`user`、`order`）。現有：`auth`（Identity）、`catalog`（Catalog）。
5. **預設所有 API 都要登入**（FallbackPolicy）。公開端點必須明確標 `[AllowAnonymous]`；管理功能標 `[Authorize(Roles = AppRoles.Admin)]`（`AppRoles` 在 SharedKernel，模組不要參照 Identity 的 `RoleNames`）。
6. **UI 不寫死色碼、字體、圓角、間距**：一律用 `frontend/src/design/tokens.css` 的 CSS 變數。視覺定位是溫暖的家用質感，**避免 SaaS 風格的藍紫漸層**。正式視覺之後由 Claude Design 提供，屆時主要替換 tokens。
7. **手機優先**：觸控目標至少 `var(--touch-target)`（44px），輸入框字級至少 16px，版面在 375px 寬度要能用。

## 新增後端模組（例如 Catalog）

```bash
cd backend
dotnet new classlib -n FamVault.Modules.Catalog -o src/Modules/Catalog/FamVault.Modules.Catalog
dotnet sln add src/Modules/Catalog/FamVault.Modules.Catalog
dotnet add src/Modules/Catalog/FamVault.Modules.Catalog reference src/FamVault.SharedKernel
dotnet add src/FamVault.Api reference src/Modules/Catalog/FamVault.Modules.Catalog
dotnet add tests/FamVault.Tests reference src/Modules/Catalog/FamVault.Modules.Catalog
```

csproj 加上 `<FrameworkReference Include="Microsoft.AspNetCore.App" />`（才能寫 Controller），並刪除 `Class1.cs`。

模組內的資料夾結構比照 Identity：

```
Domain/          實體（業務資料繼承 SharedKernel.Domain.AuditableEntity，自動填 CreatedBy/UpdatedBy）
Application/     Service、DTO（record）、FluentValidation 驗證器
Infrastructure/  <名稱>ModuleDbContext、EF 設定、Migrations
Api/             Controller：[ApiController] + [Route(ApiRoutes.Prefix + "/xxx")]
<名稱>Module.cs  實作 IModule：註冊 DbContext、Service、驗證器
```

`<名稱>Module.Register` 至少要有：

```csharp
services.AddModuleDbContext<CatalogModuleDbContext>(configuration, CatalogModuleDbContext.Schema);
services.AddScoped<ItemService>();
services.AddValidatorsFromAssembly(typeof(CatalogModule).Assembly, includeInternalTypes: true);
```

DbContext 的 `OnModelCreating` 要呼叫 `builder.HasDefaultSchema(Schema)`。
最後在 `backend/src/FamVault.Api/Program.cs` 的 `modules` 陣列加入 `new CatalogModule()`。

## 後端慣例

- **驗證**：寫 `AbstractValidator<TRequest>`，由全域 `ValidationActionFilter` 自動執行。訊息一律中文（`.WithMessage("…")`）。不要用 DataAnnotations。
- **錯誤**：可預期的錯誤丟 `SharedKernel.Errors` 的例外：`NotFoundException`（404）、`ConflictException`（409）、`BusinessRuleException`（400）、`UnauthorizedException`（401）。`AppExceptionHandler` 會轉成 ProblemDetails。不要在 Controller 內自己組錯誤回應。
- **目前使用者**：注入 `ICurrentUser`（`UserId`、`DisplayName`、`IsInRole`）。
- **時間**：注入 `TimeProvider`，不要直接用 `DateTime.Now`。稽核時間用 `DateTimeOffset`；保存期限這類日期用 `DateOnly`。
- **主鍵**：業務資料用 `int`（`AuditableEntity.Id`）；使用者是 `Guid`。新 Guid 用 `Guid.CreateVersion7()`。
- **DTO** 用 `sealed record`。JSON 欄位自動轉 camelCase。
- **清單查詢**：查詢條件寫成 `record XxxQuery`（`[FromQuery]` 綁定，也會被驗證器檢查），回傳 `SharedKernel.Web.PagedResult<T>`。範例：`ItemsController.List`。
- **刪除前檢查使用中**：被其他資料參照時丟 `ConflictException`，訊息說明有幾筆在用、該怎麼處理（範例：`CategoryService.DeleteAsync`）。
- **種子資料**：用 `HasData`，所有值（含 `CreatedAt`）必須是常數，否則每次都會產生新的 Migration（範例：`CatalogSeedData`）。
- **完整模組範例**：`Modules/Catalog`（多個 Service、查詢分頁、種子資料、跨表檢查）。

## Migration

```bash
cd backend
dotnet ef migrations add <名稱> --project src/Modules/<模組>/FamVault.Modules.<模組> --startup-project src/FamVault.Api --context <模組>ModuleDbContext --output-dir Infrastructure/Migrations
```

開發環境啟動 API 時會自動套用所有模組的 Migration（`Database:AutoMigrate=true`）。
用 sqlcmd 修改資料時要加 `-I`（QUOTED_IDENTIFIER），否則有篩選索引的資料表會失敗：
`sqlcmd -S . -E -C -I -d FamVault -Q "..."`

## 新增前端模組

建立 `frontend/src/modules/<名稱>/`：

```
index.js        匯出 { name, routes, nav }
api.js          用 '@/shared/api/http' 的 http 呼叫後端，回傳 r.data
store.js        （需要時）Pinia setup store
views/          頁面
components/     模組內元件
```

`index.js` 範例：

```js
export default {
  name: 'catalog',
  routes: [
    { path: '/items', name: 'items', component: () => import('./views/ItemsView.vue'), meta: { title: '品項' } },
  ],
  nav: [{ to: { name: 'items' }, label: '品項', icon: 'box', order: 20 }],
}
```

然後在 `frontend/src/app/modules.js` 匯入並加入 `modules` 陣列。導覽列會自動出現。

- 路由 meta：`public`（不需登入）、`requiresAdmin`、`layout: 'blank'`（不顯示導覽）、`title`、`navGroup`（子頁面要亮起哪個導覽項目，填該項目的路由名稱）。
- 導覽圖示在 `shared/components/AppIcon.vue` 的 `paths` 新增。
- 錯誤處理：API 失敗時拋出 `ApiError`，有 `message`（整體提示）與 `fieldErrors`（`{ 欄位: 訊息 }`）。表單用 `FormAlert` 顯示 message，`TextField` 的 `error` 屬性顯示欄位錯誤。
- 共用元件：`BaseButton`、`TextField`、`BaseSelect`（原生 select，`placeholder` 會加一個值為 null 的選項）、`AppDialog`（手機上變成底部面板）、`ConfirmDialog`（`onConfirm` 回傳 Promise，失敗訊息顯示在對話框內）、`PageHeader`、`FormAlert`、`AppIcon`。
- 很少變動的參考資料（例如分類、單位）放在模組 store，載入一次共用；修改後 `load({ force: true })`（範例：`modules/catalog/store.js`）。
- 長表單用獨立頁面（例如 `ItemFormView`），短表單用 `AppDialog`。

## 啟動與驗證

```bash
dotnet run --project backend/src/FamVault.Api      # http://localhost:5080，API 文件 /docs
npm --prefix frontend run dev                         # http://localhost:5173
npm --prefix frontend run dev:https                   # 手機測試鏡頭用（自簽憑證）
dotnet test backend                                   # 後端單元測試
npm --prefix frontend run build                       # 前端建置檢查
```

`.claude/launch.json` 已設定 `api` 與 `web` 兩個預覽伺服器。
修改後至少執行 `dotnet build`／`dotnet test` 與 `npm run build`；牽涉畫面時用瀏覽器實際操作，並以手機寬度（375px）檢查。
測試時建立的帳號或資料，測完要刪除。

**本機的 `FamVault` 資料庫已經有使用者的真實帳號，不要拿來測試。** 整合測試或瀏覽器測試時，讓後端連到另一個資料庫：

```bash
ConnectionStrings__Default="Server=.;Database=FamVault_Test;Trusted_Connection=True;TrustServerCertificate=True" ASPNETCORE_ENVIRONMENT=Development dotnet run --project backend/src/FamVault.Api --no-launch-profile --urls http://localhost:5081
```

前端建立 `frontend/.env.development.local`（內容 `VITE_API_PROXY_TARGET=http://localhost:5081`），重新啟動 web 預覽。
測完：刪除該檔案、停止 5081 的後端、`DROP DATABASE FamVault_Test`，再重新啟動 `api` 與 `web` 預覽。
在瀏覽器登入測試帳號會蓋掉內建瀏覽器中使用者原本的登入狀態，結束時要告知使用者。

## 文件

功能有變動時，同步更新：
- `docs/開發說明手冊.md`：架構、API、資料表、設定
- `docs/使用者說明手冊.md`：家人看得懂的操作說明（不寫技術名詞）
