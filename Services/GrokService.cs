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
            
            // 增加超时时间
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<string> GetChatResponseAsync(Chat chat)
        {
            _retryCount = 0;
            return await AttemptRequestAsync(chat);
        }

        private async Task<string> AttemptRequestAsync(Chat chat)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Sending chat request to: {_apiUrl} (Attempt: {_retryCount + 1}/{MAX_RETRIES + 1})");
                
                // 为每个请求创建新的CancellationTokenSource
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(45)); // 45秒超时
                
                // 直接使用JsonContent构建请求体
                var content = JsonContent.Create(chat);
                
                // 添加特定的请求头，以解决某些CORS问题
                var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
                request.Content = content;
                request.Headers.Add("Accept", "application/json");
                
                // 使用HttpRequestMessage进行请求，这样可以更好地控制请求
                var response = await _httpClient.SendAsync(request, cts.Token);

                // 记录响应状态码
                await _jsRuntime.InvokeVoidAsync("console.log", $"API Response Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseText = await response.Content.ReadAsStringAsync();
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
                        await _jsRuntime.InvokeVoidAsync("console.log", $"JSON parsing error: {ex.Message}");
                        return $"Error parsing API response: {ex.Message}";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _jsRuntime.InvokeVoidAsync("console.log", $"API Error: {response.StatusCode}, Content: {errorContent}");
                    
                    // 对于5xx错误进行重试
                    if (_retryCount < MAX_RETRIES && ((int)response.StatusCode >= 500 || (int)response.StatusCode == 0))
                    {
                        _retryCount++;
                        
                        // 添加延迟，避免立即重试
                        await Task.Delay(1000 * _retryCount);
                        
                        return await AttemptRequestAsync(chat);
                    }
                    
                    return $"Error: API返回错误状态码 {(int)response.StatusCode} {response.StatusCode}";
                }
            }
            catch (TaskCanceledException)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", "API request timed out");
                
                // 对超时进行重试
                if (_retryCount < MAX_RETRIES)
                {
                    _retryCount++;
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Retrying after timeout... Attempt {_retryCount}/{MAX_RETRIES}");
                    
                    // 增加延迟，避免立即重试
                    await Task.Delay(1000 * _retryCount);
                    
                    return await AttemptRequestAsync(chat);
                }
                
                return "请求超时。这可能是因为后端服务正在启动或者临时不可用。请稍后再试。";
            }
            catch (HttpRequestException ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"HTTP Request error: {ex.Message}");
                
                // 对网络错误进行重试
                if (_retryCount < MAX_RETRIES)
                {
                    _retryCount++;
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Retrying after network error... Attempt {_retryCount}/{MAX_RETRIES}");
                    
                    // 增加延迟，避免立即重试
                    await Task.Delay(1000 * _retryCount);
                    
                    return await AttemptRequestAsync(chat);
                }
                
                if (ex.Message.Contains("net_http_operation_started"))
                {
                    return "网络连接中断。这通常是由于连接超时或浏览器取消了正在进行的HTTP请求。请重新发送消息。";
                }
                
                return $"网络请求错误: {ex.Message}";
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Error calling API: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", $"Inner exception: {ex.InnerException.Message}");
                }
                
                return $"发生错误: {ex.Message}";
            }
        }
    }
}
