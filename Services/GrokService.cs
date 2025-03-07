using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using GrokBot.Models;
using Microsoft.JSInterop;

namespace GrokBot.Services
{
    public class GrokService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly string _apiUrl;
        private readonly int _timeoutSeconds = 30; // 可调整的超时设置
        private int _retryCount = 0;
        private const int MAX_RETRIES = 2;

        public GrokService(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            
            // 使用固定的确切的Render.com URL
            const string RENDER_API_URL = "https://grokbot-backend.onrender.com/api/grok/chat";
            
            // 在开发环境使用本地API，在生产环境使用Render.com API
#if DEBUG
            _apiUrl = "http://localhost:5000/api/grok/chat";
#else
            _apiUrl = RENDER_API_URL;
#endif

            // 记录使用的API URL
            Console.WriteLine($"Using API URL: {_apiUrl}");
            
            // 将URL保存到浏览器控制台以便调试
            _jsRuntime.InvokeVoidAsync("console.log", $"API URL: {_apiUrl}");
            
            // 设置更合理的超时时间
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
        }

        public async Task<string> GetChatResponseAsync(Chat chat)
        {
            _retryCount = 0;
            return await ExecuteWithRetryAsync(chat);
        }
        
        private async Task<string> ExecuteWithRetryAsync(Chat chat)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Sending chat request to: {_apiUrl} (Attempt: {_retryCount + 1})");
                
                // 使用更可靠的方式构建请求
                var messages = chat.Messages.Select(m => new
                {
                    role = m.Role,
                    content = m.Content
                }).ToList();
                
                var requestData = new
                {
                    model = "grok-2-1212",
                    messages = messages,
                    max_tokens = 1024,
                    temperature = 0.7,
                    top_p = 0.9
                };
                
                // 使用StringContent而不是JsonContent来避免某些环境中的兼容性问题
                var jsonString = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                
                // 在发送请求前先测试API可用性
                await _jsRuntime.InvokeVoidAsync("console.log", "Testing API availability...");
                
                // 设置取消令牌，以便能够在必要时取消请求
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
                
                // 发送请求
                var response = await _httpClient.PostAsync(_apiUrl, content, cts.Token);

                // 记录响应状态码
                Console.WriteLine($"API Response Status: {response.StatusCode}");
                await _jsRuntime.InvokeVoidAsync("console.log", $"API Response Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseText = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Response: {responseText}");
                    await _jsRuntime.InvokeVoidAsync("console.log", $"API Response: {responseText}");

                    try
                    {
                        var responseData = JsonSerializer.Deserialize<JsonElement>(responseText);
                        if (responseData.TryGetProperty("response", out var responseContent))
                        {
                            return responseContent.GetString() ?? "No response content";
                        }
                        
                        return "Could not extract response content from API response.";
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"JSON parsing error: {ex.Message}");
                        await _jsRuntime.InvokeVoidAsync("console.log", $"JSON parsing error: {ex.Message}");
                        return $"Error parsing API response: {ex.Message}";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Error: {response.StatusCode}, Content: {errorContent}");
                    await _jsRuntime.InvokeVoidAsync("console.log", $"API Error: {response.StatusCode}, Content: {errorContent}");
                    
                    // 尝试重试请求
                    if (_retryCount < MAX_RETRIES && (int)response.StatusCode >= 500)
                    {
                        _retryCount++;
                        await _jsRuntime.InvokeVoidAsync("console.log", $"Retrying request... Attempt {_retryCount} of {MAX_RETRIES}");
                        
                        // 增加延迟，避免立即重试
                        await Task.Delay(1000 * _retryCount);
                        
                        return await ExecuteWithRetryAsync(chat);
                    }
                    
                    // 返回更友好的错误信息
                    if ((int)response.StatusCode == 0 || (int)response.StatusCode >= 500)
                    {
                        return "抱歉，服务器暂时无法响应。Render.com的免费服务在首次调用时可能需要几分钟来启动。请稍后再试。";
                    }
                    return $"Error: API返回错误状态码 {(int)response.StatusCode} {response.StatusCode}";
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Request timed out");
                await _jsRuntime.InvokeVoidAsync("console.log", "Request timed out");
                
                // 尝试重试超时的请求
                if (_retryCount < MAX_RETRIES)
                {
                    _retryCount++;
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Retrying after timeout... Attempt {_retryCount} of {MAX_RETRIES}");
                    await Task.Delay(1000 * _retryCount);
                    return await ExecuteWithRetryAsync(chat);
                }
                
                return "请求超时，这可能是因为Render.com的免费服务正在启动中。请稍后再试。";
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error calling API: {ex.Message}");
                await _jsRuntime.InvokeVoidAsync("console.log", $"HTTP request error: {ex.Message}");
                
                // 尝试重试网络错误
                if (_retryCount < MAX_RETRIES)
                {
                    _retryCount++;
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Retrying after network error... Attempt {_retryCount} of {MAX_RETRIES}");
                    await Task.Delay(1000 * _retryCount);
                    return await ExecuteWithRetryAsync(chat);
                }
                
                return "网络连接错误，无法连接到API服务。这可能是因为后端服务暂时不可用或者正在启动中。";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling API: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error calling API: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Inner exception: {ex.InnerException.Message}");
                }
                
                // 返回更友好的错误信息
                if (ex.Message.Contains("net_http") || ex.Message.Contains("network"))
                {
                    return "网络错误: 无法连接到后端服务。Render.com的免费服务可能正在启动中，请稍后再试。";
                }
                
                return $"发生错误: {ex.Message}";
            }
        }
    }
}
