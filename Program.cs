using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GrokBot;
using GrokBot.Services;
using Microsoft.JSInterop;
using System.Text.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 获取正确的base URL取决于环境
var baseAddress = builder.HostEnvironment.BaseAddress;
Console.WriteLine($"Base Address: {baseAddress}");

// 添加HttpClient服务
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });

// 添加应用服务
builder.Services.AddScoped<GrokService>();
builder.Services.AddScoped<ChatStorageService>();

// 注册启动任务
builder.Services.AddScoped<IStartupTask, StorageCleanupTask>();

// 构建并运行应用
var host = builder.Build();

// 运行所有启动任务
var startupTasks = host.Services.GetServices<IStartupTask>();
foreach (var task in startupTasks)
{
    await task.ExecuteAsync();
}

await host.RunAsync();

// 启动任务接口
public interface IStartupTask
{
    Task ExecuteAsync();
}

// 存储清理启动任务
public class StorageCleanupTask : IStartupTask
{
    private readonly IJSRuntime _jsRuntime;

    public StorageCleanupTask(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            // 尝试清除LocalStorage中的所有数据
            await _jsRuntime.InvokeVoidAsync("localStorage.clear");
            Console.WriteLine("LocalStorage cleared successfully.");
            
            // 设置一个标识，表示已进行清理
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "storage_cleaned", "true");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error clearing localStorage: {ex.Message}");
        }
    }
}
