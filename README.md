> **2025 年历史演示。** GitHub Pages 能打开只代表静态前端仍在，不代表聊天链可用；
> 仓库归档后部署已冻结，Render 后端也不提供持续可用性保证。

# GrokBot

GrokBot是一个由Grok AI驱动的聊天应用程序。它为与Grok AI语言模型交互提供了用户友好的界面。

## 前端（GitHub Pages）

该仓库包含GrokBot的前端代码。前端是一个Blazor WebAssembly应用程序，与托管在Render.com上的后端API通信。

### GitHub Pages部署

最后一个版本曾由 GitHub Actions 自动部署到 GitHub Pages；仓库归档后不再维护这条部署链。

您可以在https://alicelJY.github.io/grokbot/ 访问已部署的应用程序

### 本地开发

1. 克隆仓库
2. 导航到项目目录
3. 运行`dotnet restore`
4. 运行`dotnet run`

应用程序将在`https://localhost:5001`和`http://localhost:5000`上可用。

## 后端（Render.com）

最后一个版本把后端 API 部署在 Render.com，用来代理 Grok AI 调用；这是历史架构说明，
不代表当前服务仍可用。

后端代码可在https://github.com/AliceLJY/grokbot-backend 获取
