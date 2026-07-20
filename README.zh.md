<p align="center">
  <img src="https://github.com/View12138/GitPlus/raw/main/GitPlus/Assets/Logo.png" alt="GitPlus 图标" width="128" />
</p>

<h1 align="center">Git +</h1>

<p align="center">
  <strong>Visual Studio Git 增强扩展</strong>
  <br />
  在 VS 内置 Git 工具之上，提供工具栏按钮、自动 Fetch、智能同步和约定式提交功能。
</p>

<p align="center">
  <a href="https://github.com/View12138/GitPlus/blob/main/README.md">English Documentation</a>
</p>

---

## ✨ 功能

### 🔄 拉取（自动储藏）

一键拉取，自动处理本地更改。**Pull with Stash（拉取+储藏）** 按钮被注入到 Git 更改窗口工具栏，紧邻原生 Pull 按钮。点击后：

1. 通过 `git status` 检查本地更改
2. 如有本地更改，自动储藏（`git stash push`）
3. 执行 `git pull`（可选 `--rebase`）
4. 恢复储藏（`git stash pop`）

告别"存在未提交的更改，无法拉取"的错误——快速同步代码而不中断工作流。

### ⏱️ 自动 Fetch

按可配置的时间间隔，在后台定期执行 `git fetch --all --prune`。无需手动操作即可保持与远程仓库同步。自动 Fetch 的间隔信息会显示在 Fetch 按钮的提示文本中。

### 📝 约定式提交

将约定式提交模板直接插入 Git 提交消息框。**插入约定式提交模板** 按钮被注入到 Git 更改窗口，紧邻工作项链接按钮。支持：

- 从预定义的提交类型中选择（`feat`、`fix`、`docs`、`style`、`refactor`、`perf`、`test`、`chore`、`ci`、`build`、`revert`），每种类型均附带 emoji 和描述
- 可选的**范围**支持 — 从解决方案根目录中的可自定义范围 JSON 文件读取
- **BREAKING CHANGE** 尾注插入
- 类型替换 — 选择不同类型可替换消息中已有的类型
- 通过 `GENERATE OPTIONFILE` 菜单项生成选项模板文件

### ✅ 约定式提交语法校验

对提交消息进行实时语法校验，遵循 [Conventional Commits](https://www.conventionalcommits.org/) 规范。当通过 `ConventionalCommitOption.json` 选项文件启用**严格校验**后，扩展将：

- **实时解析**提交消息，使用自研的 lexer/parser 完整理解约定式提交语法（类型、范围、描述、正文、尾注）
- **高亮显示**语法错误和警告，直接在提交文本框中以波浪下划线标注 — 红色表示错误，黄色表示警告
- 格式无效时以**红色边框**勾勒提交消息区域
- **禁用**提交按钮，直至所有错误均已解决
- **显示**诊断消息（CC001–CC014），精确描述问题所在

校验器检查以下内容：
- 缺失或无效的提交类型
- 类型/范围后缺少冒号或空格
- 范围括号格式错误
- 缺少描述文本
- 无效的尾注格式（如 `BREAKING CHANGE`、`Reviewed-by`）
- 意外的多余空行或空格

GitPlus 支持**基于仓库的提交规范配置**。`ConventionalCommitOption.json` 可直接提交到 Git 仓库，使团队成员共享统一的 Commit Message 规则。该文件可从**插入约定式提交模板**菜单生成，用于定义自定义提交类型、允许的范围以及项目专属的尾注键 — 确保整个团队保持一致的提交规范。

### ⚙️ 可配置选项

所有选项可通过 **工具 → 选项 → Git +** 访问：

| 类别       | 选项                 | 默认值                        | 描述                         |
|------------|----------------------|-------------------------------|------------------------------|
| 常规       | 超时（秒）           | 30                            | Git 操作超时时间             |
| 常规       | Git 文件路径         | *(系统 PATH)*                 | 自定义 `git.exe` 路径        |
| 自动 Fetch | 启用自动 Fetch       | ✅ 开                          | 开启/关闭后台自动 Fetch      |
| 自动 Fetch | Fetch 间隔（分钟）   | 5                             | 自动 Fetch 调用间隔          |
| 拉取       | 拉取时使用 Rebase    | ✅ 开                          | 使用 `--rebase` 代替合并     |
| 拉取       | 显示自动拉取按钮     | ✅ 开                          | 显示/隐藏自动拉取按钮        |
| 提交       | 显示约定式提交按钮   | ✅ 开                          | 显示/隐藏约定式提交按钮      |
| 提交       | 使用范围             | ✅ 开                          | 在提交消息中包含范围         |
| 提交       | 约定式提交范围文件名 | ConventionalCommitScopes.json | 用于提交消息范围的文件名     |
| 日志       | 日志级别             | 信息                          | Git + 输出窗格的日志详细程度 |

### 🌐 本地化

扩展界面支持英文和简体中文（zh-Hans）两种语言。

---

## 📸 截图

<p align="center">
  <img width="776px" src="https://github.com/View12138/GitPlus/raw/main/GitPlus/Assets/Screenshots1.gif" alt="GitPlus" style="vertical-align: top;" />
</p>

*注入到 Git 更改窗口中的 **Pull with Stash（拉取+储藏）** 按钮、**约定式提交** 按钮以及实时 **语法校验** 功能。*

---

## 📦 安装

### 前提条件

- **Visual Studio 2022**（版本 17.0 或更高）
- **.NET Framework 4.7.2** 或更高

### 通过 VSIX 安装

从 [Releases](https://github.com/View12138/GitPlus/releases/latest) 下载 `.vsix` 文件，双击安装。或从源码构建：

```powershell
dotnet build GitPlus.slnx -c Release
```

生成的 `.vsix` 文件位于：
```
GitPlus/bin/Release/net472/GitPlus.vsix
```

---

## 🛠️ 开发

### 技术栈

- **目标框架**：`net472`（VS 进程内扩展）
- **语言**：C# 14（record、模式匹配、file-scoped namespace）
- **核心库**：
  - `Microsoft.VisualStudio.SDK` — VS 扩展性
  - `CommunityToolkit.Mvvm` — `AsyncRelayCommand`
  - `Lombok.NET` — `[RequiredArgsConstructor]` 源码生成注入
  - `Microsoft.Extensions.DependencyInjection` — DI 容器
  - `Microsoft.Extensions.Logging.Abstractions` — 结构化日志

### 项目结构

```
GitPlus/
├── GitPlusPackage.cs              # AsyncPackage 入口
├── Configurations/
│   └── GitPlusOptionPage.cs       # 工具 → 选项 对话框页
├── Injectors/
│   ├── InjectorBase.cs            # UI 注入器抽象基类
│   ├── GitWindowActionButtonPanelInjector.cs
│   └── GitWorkItemActionStackPanelInjector.cs
├── Services/
│   ├── WindowWatcher.cs           # DTE 窗口生命周期事件
│   ├── GitCommandService.cs       # git.exe 进程封装
│   └── AutoFetchService.cs        # 定时自动 Fetch
├── Commons/
│   ├── Extensions.cs              # DI / WPF VisualTree 辅助方法
│   ├── FileBrowserEditor.cs       # 文件浏览器 UITypeEditor
│   ├── GitResult.cs               # Git 操作结果模型
│   ├── GitWindowLocator.cs        # VisualTree 元素定位器
│   ├── GitWindowViewModelExtensions.cs
│   ├── LocalizedAttributes.cs     # 本地化特性类
│   └── ConventionalCommitSyntaxs/ # 提交消息 lexer/parser/诊断
├── Assets/
│   ├── conventional-commits-rules.json
│   └── conventional-commits-rules.zh-Hans.json
└── Resources/
    ├── GitButtonStyle.xaml        # ImageButton 控件模板
    └── Icons.xaml                 # DrawingImage 矢量图标

GitPlus.Tests/                     # xunit + Moq 测试项目
```

### 构建

```powershell
dotnet build GitPlus.slnx
```

### 运行测试

```powershell
dotnet test GitPlus.slnx
```

### 调试 (F5)

在 Visual Studio 中打开解决方案，将 `GitPlus` 设为启动项目，按 **F5** — 这会启动 VS 实验实例并加载扩展。

---

## 🧪 测试

`GitPlus.Tests` 项目使用 **xunit** + **Moq**，目标框架为 `net472`，并引用 WPF 程序集以支持基于 `DependencyObject` 的测试。

---

## 📄 许可证

本项目基于 MIT 许可证开源 — 详见 [LICENSE.txt](https://github.com/View12138/GitPlus/blob/main/LICENSE.txt)。

---

<p align="center">
  <sub>Made with ❤️ by <strong>View</strong></sub>
</p>
