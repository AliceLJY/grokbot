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
builder.Services.AddScoped<IStartupTask, StorageMaintenanceTask>();

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

// 修改的存储维护任务 - 保留聊天数据
public class StorageMaintenanceTask : IStartupTask
{
    private readonly IJSRuntime _jsRuntime;

    public StorageMaintenanceTask(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            // 检查存储是否需要维护，而不是直接清除
            var storageNeedsMaintenance = await _jsRuntime.InvokeAsync<bool>(
                "eval", 
                @"function checkStorageMaintenance() {
                    try {
                        // 检查存储使用情况
                        const used = Object.keys(localStorage).reduce((acc, key) => {
                            return acc + (localStorage[key] ? localStorage[key].length : 0);
                        }, 0);
                        
                        // 如果使用量超过一定比例 (例如80%), 则需要维护
                        const quota = 5 * 1024 * 1024; // 5MB
                        const percentage = Math.round((used / quota) * 100);
                        
                        console.log('存储使用率: ' + percentage + '%');
                        return percentage > 80;
                    } catch(e) {
                        console.error('检查存储时出错:', e);
                        return false;
                    }
                }; 
                checkStorageMaintenance();"
            );
            
            if (storageNeedsMaintenance)
            {
                Console.WriteLine("需要进行存储维护。执行选择性清理...");
                
                // 选择性清理，保留聊天数据
                await _jsRuntime.InvokeVoidAsync(
                    "eval", 
                    @"function cleanupStorage() {
                        try {
                            // 保留聊天数据
                            const chatDataKey = 'grokbot_chats';
                            const chatData = localStorage.getItem(chatDataKey);
                            
                            // 清除除聊天数据外的所有内容
                            const keysToRemove = Object.keys(localStorage).filter(key => key !== chatDataKey);
                            keysToRemove.forEach(key => localStorage.removeItem(key));
                            
                            console.log('已选择性清除存储，保留聊天数据');
                            return true;
                        } catch(e) {
                            console.error('清理存储时出错:', e);
                            return false;
                        }
                    };
                    cleanupStorage();"
                );
            }
            else
            {
                Console.WriteLine("不需要进行存储维护。");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"管理localStorage时出错: {ex.Message}");
        }
    }
}
