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
        }

        public async Task<string> GetChatResponseAsync(Chat chat)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.log", $"Sending chat request to: {_apiUrl}");
                Console.WriteLine($"Sending request to {_apiUrl}");
                
                // 添加超时设置
                _httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                // 直接使用JsonContent构建请求体
                var content = JsonContent.Create(chat);
                
                // 发送请求
                var response = await _httpClient.PostAsync(_apiUrl, content);

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
                    return $"Error: API返回错误状态码 {(int)response.StatusCode} {response.StatusCode}";
                }
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
                
                return $"发生错误: {ex.Message}";
            }
        }
    }
}
