# BedrockLauncher.Core  
A Minecraft Bedrock Launch Core  

# About the Core  
---

# 🎮 BedrockLauncher.Core  

> **Minecraft Bedrock Edition Core Management Library**  
> A .NET core library for installing, switching, launching, and uninstalling Minecraft UWP versions. Supports multi-version management, background customization, automatic dependency completion, and developer mode control.  

📦 Install the core library via NuGet: [BedrockLauncher.Core](https://www.nuget.org/packages/BedrockLauncher.Core/)  

![License](https://img.shields.io/badge/license-GPL-blue.svg)  
[![.NET](https://img.shields.io/badge/.NET-10.0%2B-orange)](https://dotnet.microsoft.com/download)  

---

## 📌 Introduction  

`BedrockLauncher.Core` is a lightweight, high-performance .NET library designed for **Minecraft Bedrock** version management. It enables third-party launchers to implement the following features:  

> [!WARNING]  
> The program **must be run with administrator privileges**; otherwise, it cannot access system resources or launch normally.  

- ✅ Automatically download and install specified versions of Minecraft  
- ✅ Switch between different game versions (supports multiple instances)  
- ✅ Start/close game instances  
- ✅ Customize launch screen background  
- ✅ Automatically enable Windows Developer Mode  
- ✅ Automatically install VC++ and UWP runtime libraries  
- ✅ Multi-threaded, resumable downloader  
- ✅ Complete installation progress and status callbacks  

---

## 🔧 Core Features  

| Feature | Description |  
|---------|-------------|  
| 📦 Version Installation | Download and register Minecraft packages |  
| 🔁 Version Switching | Support for multiple version coexistence and quick switching |  
| ▶️ Game Launch | Use system protocols to launch a specific version |  
| ⏹️ Game Closure | Safely terminate running game processes |  
| 🖼️ Launch Background Editing | Modify `AppxManifest.xml` to customize the launch image |  
| ⚙️ Automatic Environment Configuration | Automatically enable Developer Mode & install runtime libraries |  
| 📈 Progress Feedback | Provide callbacks for download, deployment, and installation processes |  

---

## 🚀 Quick Start  

### 1. Install the NuGet Package  

```shell  
dotnet add package BedrockLauncher.Core  
```  

### 2. Initialize the Core  

```csharp  
using BedrockLauncher.Core;  

// Initialize core components  
var bedrockCore = new BedrockCore();  
bedrockCore.Init();  
```  
---

## 🛠️ Technology Stack  

- **Language**: C# 14.0+  
- **Platform**: Windows 10 / 11  
- **Framework**: .NET 10+  
- **Dependencies**:  
  - `Windows.Management.Deployment` (app deployment)  
  - `System.Threading.Tasks`  
  - `System.Net.Http`  

---

## 📄 License  

This project is licensed under the [MIT License](LICENSE). It can be freely used for learning, commercial projects, or secondary development.  

```text  
Copyright (c) 2025 BedrockLauncher Team  

Permission is hereby granted, free of charge, to any person obtaining a copy  
of this software and associated documentation files (the "Software"), to deal  
in the Software without restriction, including without limitation the rights  
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell  
copies of the Software...  
```  

---

## 🤝 Contributing  

We welcome Issues and Pull Requests!  

- 💬 Report Bugs → [Issues](https://github.com/Round-Studio/BedrockLauncher.Core/issues)  
- ✨ Suggest Features → [Discussions](https://github.com/Round-Studio/BedrockLauncher.Core/discussions)  
- 🛠️ Contribute to Development → Fork the project and submit a PR  

---

## 📮 Contact Us  

- GitHub: [@Round Studio](https://github.com/Round-Studio/)
- Docs: [BedrockLauncher.Core Docs](https://docs.roundstudio.top/docs/product/blc)
