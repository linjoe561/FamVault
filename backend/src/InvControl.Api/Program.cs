using InvControl.Modules.Identity;
using InvControl.SharedKernel;
using InvControl.SharedKernel.Modules;
using InvControl.SharedKernel.Persistence;
using InvControl.SharedKernel.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 新增功能模組時，在這裡加入一行即可。
IModule[] modules =
[
    new IdentityModule(),
];

builder.Services.AddSharedKernel();
foreach (var module in modules)
{
    module.Register(builder.Services, builder.Configuration);
}

var mvc = builder.Services.AddControllers(options =>
{
    // 必填檢查交給 FluentValidation，訊息才會是中文且格式一致。
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.Filters.Add<ValidationActionFilter>();
});
foreach (var module in modules)
{
    mvc.AddApplicationPart(module.GetType().Assembly);
}

// 預設所有 API 都要登入，不需要登入的端點用 [AllowAnonymous] 標示。
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "InvControl 家庭庫存 API";
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    };
    document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] }];
    return Task.CompletedTask;
}));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference("/docs").AllowAnonymous();
}

if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    await app.Services.MigrateModuleDatabasesAsync();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet($"/{ApiRoutes.Prefix}/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();

public partial class Program;
