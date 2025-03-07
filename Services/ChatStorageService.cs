using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using GrokBot.Models;
using System.Text.Json;

namespace GrokBot.Services
{
    public class ChatStorageService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string CHATS_KEY = "grokbot_chats";
        private const int MAX_STORED_CHATS = 5; // 严格限制存储的聊天数量
        private const int MAX_MESSAGES_PER_CHAT = 10; // 限制每个聊天的消息数量

        public ChatStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<List<Chat>> GetAllChatsAsync()
        {
            try
            {
                // 尝试读取存储数据
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", CHATS_KEY);
                if (string.IsNullOrEmpty(json))
                    return new List<Chat>();

                // 控制台记录存储大小以便调试
                await _jsRuntime.InvokeVoidAsync("console.log", $"Storage size: {json.Length} bytes");
                
                try
                {
                    return JsonSerializer.Deserialize<List<Chat>>(json) ?? new List<Chat>();
                }
                catch (JsonException ex)
                {
                    // 如果解析失败，清除存储并返回空列表
                    await _jsRuntime.InvokeVoidAsync("console.error", $"JSON parsing error: {ex.Message}");
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", CHATS_KEY);
                    return new List<Chat>();
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Error loading chats: {ex.Message}");
                return new List<Chat>();
            }
        }

        public async Task SaveChatAsync(Chat chat)
        {
            try
            {
                // 获取所有聊天
                var chats = await GetAllChatsAsync();
                
                // 限制消息数量，仅保留最新的N条
                if (chat.Messages.Count > MAX_MESSAGES_PER_CHAT)
                {
                    chat.Messages = chat.Messages
                        .Skip(chat.Messages.Count - MAX_MESSAGES_PER_CHAT)
                        .Take(MAX_MESSAGES_PER_CHAT)
                        .ToList();
                    
                    await _jsRuntime.InvokeVoidAsync("console.log", 
                        $"Truncated chat messages to {chat.Messages.Count} messages");
                }
                
                // 检查是否已存在此聊天
                var existingChat = chats.FirstOrDefault(c => c.Id == chat.Id);
                if (existingChat != null)
                {
                    // 更新现有聊天
                    var index = chats.IndexOf(existingChat);
                    chats[index] = chat;
                }
                else
                {
                    // 添加新聊天
                    chats.Add(chat);
                }
                
                // 仅保留最新的N个聊天记录
                if (chats.Count > MAX_STORED_CHATS)
                {
                    var sortedChats = chats
                        .OrderByDescending(c => 
                            c.Messages.Count > 0 ? 
                            c.Messages.Max(m => m.Timestamp) : 
                            DateTime.MinValue)
                        .Take(MAX_STORED_CHATS)
                        .ToList();
                    
                    chats = sortedChats;
                    await _jsRuntime.InvokeVoidAsync("console.log", 
                        $"Truncated chats to {chats.Count} chats");
                }
                
                // 序列化并保存
                var json = JsonSerializer.Serialize(chats);
                
                // 检查存储大小，记录到控制台
                await _jsRuntime.InvokeVoidAsync("console.log", $"Storing {json.Length} bytes to localStorage");
                
                // 如果数据太大，清除存储
                if (json.Length > 2000000) // 2MB 限制
                {
                    await _jsRuntime.InvokeVoidAsync("console.warn", "Storage size exceeds 2MB, clearing all chats");
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", CHATS_KEY);
                    
                    // 只保存当前聊天
                    var singleChatJson = JsonSerializer.Serialize(new List<Chat> { chat });
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CHATS_KEY, singleChatJson);
                    return;
                }
                
                // 保存到本地存储
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CHATS_KEY, json);
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Error saving chat: {ex.Message}");
                
                // 如果保存失败，尝试清除存储并重新保存单个聊天
                try
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.clear");
                    var newList = new List<Chat> { chat };
                    var json = JsonSerializer.Serialize(newList);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CHATS_KEY, json);
                }
                catch (Exception clearEx)
                {
                    await _jsRuntime.InvokeVoidAsync("console.error", 
                        $"Failed to clear and save chat: {clearEx.Message}");
                }
            }
        }

        public async Task<Chat?> GetChatByIdAsync(string id)
        {
            var chats = await GetAllChatsAsync();
            return chats.FirstOrDefault(c => c.Id == id);
        }

        public async Task DeleteChatAsync(string id)
        {
            try
            {
                var chats = await GetAllChatsAsync();
                var chatToRemove = chats.FirstOrDefault(c => c.Id == id);
                
                if (chatToRemove != null)
                {
                    chats.Remove(chatToRemove);
                    var json = JsonSerializer.Serialize(chats);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CHATS_KEY, json);
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Error deleting chat: {ex.Message}");
            }
        }
        
        public async Task ClearAllChatsAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", CHATS_KEY);
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Error clearing chats: {ex.Message}");
            }
        }
    }
}
