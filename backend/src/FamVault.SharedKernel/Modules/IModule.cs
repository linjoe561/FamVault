using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FamVault.SharedKernel.Modules;

/// <summary>
/// 功能模組的進入點。Api 啟動時會呼叫 <see cref="Register"/> 註冊模組的服務，
/// 並把模組所在的組件加入 MVC，讓模組內的 Controller 能被找到。
/// </summary>
public interface IModule
{
    string Name { get; }

    void Register(IServiceCollection services, IConfiguration configuration);
}
