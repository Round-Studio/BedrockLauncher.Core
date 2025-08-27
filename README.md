# BedrockLauncher.Core
A Minecraft Bedrock Launch Core
下载协议版本 https://github.com/Open-MBC/MineCraft-Bedrock-Download-Protocol
# 关于核心
---

# 🎮 BedrockLauncher.Core

> **Minecraft Bedrock Edition 核心管理库**  
> 用于安装、切换、启动、卸载 Minecraft UWP 版本的 .NET 核心库，支持多版本管理、背景自定义、自动依赖补全和开发者模式控制。

 📦 可通过 NuGet 安装核心库：[BedrockLauncher.Core](https://www.nuget.org/packages/BedrockLauncher.Core/)

 ![License](https://img.shields.io/badge/license-MIT-blue.svg)
[![.NET](https://img.shields.io/badge/.NET-8.0%2B-orange)](https://dotnet.microsoft.com/download)
---

## 📌 简介

`BedrockLauncher.Core` 是一个轻量级、高性能的 .NET 库，专为 **Minecraft Bedrock（UWP 版）** 的版本管理而设计。它允许第三方启动器实现以下功能：
> [!WARNING]  
> 本项目提供的多为同步函数。  
> 必须以 **管理员权限** 运行程序，否则无法访问系统资源或正常启动。

- ✅ 自动下载并安装指定版本的 Minecraft
- ✅ 切换不同游戏版本（多开支持）
- ✅ 启动/关闭游戏实例
- ✅ 自定义启动画面背景
- ✅ 自动开启 Windows 开发者模式
- ✅ 自动补全 VC++ 与 UWP 运行库
- ✅ 多线程断点续传下载器
- ✅ 完整的安装进度与状态回调

---

## 🔧 核心功能

| 功能 | 说明 |
|------|------|
| 📦 版本安装 | 下载并注册 Minecraft UWP 包 |
| 🔁 版本切换 | 支持多个版本共存与快速切换 |
| ▶️ 游戏启动 | 调用系统协议启动指定版本 |
| ⏹️ 游戏关闭 | 安全终止运行中的游戏进程 |
| 🖼️ 启动背景编辑 | 修改 `AppxManifest.xml` 实现自定义启动图 |
| ⚙️ 自动环境配置 | 自动启用开发者模式 & 安装运行库 |
| 📈 进度反馈 | 提供下载、部署、安装全过程回调 |
---

## 🚀 快速开始

### 1. 安装 NuGet 包

```shell
dotnet add package BedrockLauncher.Core
```

### 2. 初始化核心

```csharp
using BedrockLauncher.Core;

// 初始化核心组件
var bedrockCore = new BedrockCore();
bedrockCore.Init();
```

### 3. Example

```csharp
var bedrockCore = new BedrockCore();
var coreOptions = new CoreOptions();
bedrockCore.Options = coreOptions;
bedrockCore.Init();
var versionInformations = VersionHelper.GetVersions("https://data.mcappx.com/v1/bedrock.json");
var cts = new CancellationTokenSource();
InstallCallback callback = new InstallCallback()
{
    zipProgress = new Progress<ZipProgress>((progress =>
    {
        Console.WriteLine(progress.ToString());
    })),
    CancellationToken = cts.Token,
    downloadProgress = (new Progress<DownloadProgress>((p =>
    {
        if (p.TotalBytes > 0)
        {
            Console.Write($"\r下载进度: {p.ProgressPercentage:F2}% ({p.DownloadedBytes / (1024.0 * 1024):F2} MB / {p.TotalBytes / (1024.0 * 1024):F2} MB)");
        }
        else
        {
            Console.Write($"\r已下载: {p.DownloadedBytes / (1024.0 * 1024):F2} MB (总大小未知)");
        }
    }))),
    registerProcess_percent = ((s, u) =>
    {

        Console.WriteLine(s + u);
    }),
    result_callback = ((status, exception) =>
    {

    }),
    install_states = (states =>
    {
        Console.WriteLine(states); ;
    })
};
var information = versionInformations[0];
bedrockCore.InstallVersion(information.Variations[0],VersionType.Release,"./1.appx","genshin","1.21.21", callback);
bedrockCore.ChangeVersion(Path.Combine(Directory.GetCurrentDirectory(), "testDir"), callback);
bedrockCore.LaunchGame(VersionType.Preview);
```

### 4. 启动游戏

```csharp
// 先切换版本
 BedrockCore.ChangeVersion(@"C:\MyLauncher\Versions\1.20.15", callback);

// 再启动
 BedrockCore.LaunchGame(VersionType.Custom);
```
---

## 🛠️ 技术栈

- **语言**：C# 9.0+
- **平台**：Windows 10 / 11（UWP）
- **框架**：.NET 8+
- **依赖**：
  - `Windows.Management.Deployment`（应用部署）
  - `System.Threading.Tasks`
  - `System.Net.Http`
---

## 📁 项目结构（简要）

```
BedrockLauncher.Core/
├── Core/                   # 核心逻辑（BedrockCore）
├── JsonHandle/            # 版本信息数据模型
├── CoreOption/            # 配置选项
├── FrameworkComplete/     # 运行库自动安装
├── Native/                # WinRT API 调用封装
├── ManifestEditor/        # AppxManifest 修改工具
├── GameBackGroundEditer/  # 启动背景配置
└── Utils/                 # 下载器、进度类等
```

---

## 📄 许可证

本项目采用 [MIT License]，欢迎用于学习、商业项目或二次开发。

```text
Copyright (c) 2025 BedrockLauncher Team

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files, to deal in the Software
without restriction, including without limitation the rights to use, copy,
modify, merge, publish, distribute, sublicense, and/or sell copies of the
Software...
```
---

## 🤝 贡献

欢迎提交 Issue 或 Pull Request！

- 💬 报告 Bug → [Issues](https://github.com/Round-Studio/BedrockLauncher.Core/issues)
- ✨ 提出建议 → [Discussions](https://github.com/Round-Studio/BedrockLauncher.Core/discussions)
- 🛠️ 参与开发 → Fork 本项目并提交 PR
---

## 📮 联系我们

- GitHub: [@BedrockLauncher](https://github.com/Round-Studio/)


