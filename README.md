> **[Archived]** This repository is no longer maintained and is kept for history only.

# GrokBot

GrokBot是一个由Grok AI驱动的聊天应用程序。它为与Grok AI语言模型交互提供了用户友好的界面。

## 前端（GitHub Pages）

该仓库包含GrokBot的前端代码。前端是一个Blazor WebAssembly应用程序，与托管在Render.com上的后端API通信。

### GitHub Pages部署

当更改推送到主分支时，前端会自动部署到GitHub Pages。部署过程由GitHub Actions工作流处理。

您可以在https://alicelJY.github.io/grokbot/ 访问已部署的应用程序

### 本地开发

1. 克隆仓库
2. 导航到项目目录
3. 运行`dotnet restore`
4. 运行`dotnet run`

应用程序将在`https://localhost:5001`和`http://localhost:5000`上可用。

## 后端（Render.com）

后端API托管在Render.com上。后端处理与Grok AI API的通信，并为前端提供代理。

后端代码可在https://github.com/AliceLJY/grokbot-backend 获取