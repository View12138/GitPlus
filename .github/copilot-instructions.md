# GitPlus — VS Extension 开发约定

> **编写原则**：本指南只展示正确做法，不包含错误/反例代码示例。规则用肯定句式描述，避免"不要 xxx"的否定表述。反模式对照见文末表格。

## 技术栈

- 目标框架 `net472`（VS 进程内扩展）
- LangVersion 14（record、file-scoped namespace、switch expression、is-not pattern 可用）
- `<UseWPF>true</UseWPF>` 代替逐个引用 WPF 程序集
- `<Nullable>enable</Nullable>` 全局启用

### 包清单

| 包 | 用途 |
|---|------|
| `Microsoft.VisualStudio.SDK` (`ExcludeAssets="runtime"`) | VS SDK 编译期 |
| `Microsoft.VSSDK.BuildTools` | VSIX 构建 |
| `CommunityToolkit.Mvvm` | `RelayCommand` / `AsyncRelayCommand` |
| `Lombok.NET` | `[RequiredArgsConstructor]` 生成构造函数 |
| `Microsoft.Extensions.DependencyInjection` | DI 容器 |
| `Microsoft.Extensions.Logging.Abstractions` | `ILogger` 接口 |

### 抑制的警告

`VSTHRD010 VSTHRD200 VSSDK007 VSTHRD002 VSTHRD001 VSTHRD103 CS8618`

## 命名规范

### 禁止下划线前缀

所有命名（字段、属性、变量、方法、类等）**不得以下划线 `_` 开头**。特殊情况（如与外部系统对接、序列化字段名等）可使用下划线作为单词分隔符（如 `some_field_name`），但不能用作前缀。

> **例外**：XAML 元素名称（如 `PART_Image`）中的下划线前缀是 WPF 约定，不受此限制。

### 变量名使用全拼

变量、参数、方法命名尽量使用完整的单词拼写，避免缩写，保证可读性。

> **例外**：行业通用缩写（如 `Id`、`Xml`、`Http`、`Uri`、`Dto`）以及 VS SDK 类型名（如 `DTE`、`IVsUIShell`）允许直接使用。

## 项目结构

```
GitPlus/
├── GitPlusPackage.cs              # AsyncPackage 入口 — DI 初始化 + 事件绑定 + Injector 调度
├── Configurations/
│   └── GitPlusOptionPage.cs       # DialogPage（Tools → Options）+ GitPlusOption record
├── Injectors/
│   ├── InjectorBase.cs            # 抽象基类 — CanInject + InjectAsync
│   └── GitWindowActionButtonPanelInjector.cs  # Git 窗口按钮注入器
├── Services/
│   ├── WindowWatcher.cs           # DTE.Events.WindowEvents 生命周期监听
│   ├── GitCommandService.cs       # git.exe 进程包装
│   └── AutoFetchService.cs        # 定时 git fetch
├── Commons/
│   ├── Extensions.cs              # 全局扩展方法（ServiceProvider / WPF / String）
│   ├── GitResult.cs               # Git 操作结果模型
│   ├── GitWindowLocator.cs        # 静态工具类 — VisualTree 定位 Git 窗口元素
│   ├── GitWindowViewModelExtensions.cs  # 反射扩展 — 调用 VS 内部 ViewModel 方法
│   └── OutputWindowLogger.cs      # ILogger → VS Output Window 适配
├── Resources/
│   ├── GitButtonStyle.xaml        # ImageButton 按钮样式（ControlTemplate + VS 主题色 Trigger）
│   ├── Icons.xaml                 # DrawingImage 图标资源（PullWithStash 等）
│   └── Logo.png                   # 扩展图标
└── Properties/
    ├── GlobalUsings.cs            # 全局 using
    └── IsExternalInit.cs         # net472 兼容 record 的 init 访问器

GitPlus.Tests/
├── GitPlus.Tests.csproj           # xunit + Moq 测试项目
└── GitResultTests.cs              # GitResult 工厂方法单元测试
```

## 架构原则

### DI：无接口注册

**非必要不为服务创建接口**。所有服务直接用具体类注册。DI 注册在 `InitializeAsync` 中内联完成，不使用独立的扩展方法。

### Lombok.NET 构造函数生成

所有服务类用 `[RequiredArgsConstructor]` + `sealed partial`。标注为 `readonly` 的字段自动纳入构造函数，非 `readonly` 字段（如初始化 `new()` 的）自动跳过。

### 日志

统一使用 `Microsoft.Extensions.Logging.ILogger`，不是自定义接口。

### GlobalUsings.cs

所有高频导入放入 `Properties/GlobalUsings.cs`，不要在单个文件重复。已全局导入的命名空间涵盖 system、microsoft extensions、CommunityToolkit.Mvvm、项目自身命名空间（`GitPlus.Commons`、`GitPlus.Configurations`、`GitPlus.Injectors`、`GitPlus.Services`）以及 `Lombok.NET`。

### Extensions 类：简化 DI 访问

`Commons/Extensions.cs` 维护一个静态 `IServiceProvider`，通过 `Extensions.GetRequiredService<T>()` / `Extensions.GetService<T>()` 静态方法访问容器。还提供 `GetService(Type)` 和 `GetRequiredService(Type)` 重载用于反射场景。

## Package 初始化模式

DI 注册在 `InitializeAsync` 中内联完成：`ServiceCollection` 注册 `ILogger`、`WindowWatcher`、`GitCommandService`、`AutoFetchService`，并通过 `AddTransient` + `GetDialogPage` 注入 `GitPlusOption` record、通过 `AddTransient` + `GetGlobalService` 注入 `DTE` 和 `IVsOutputWindow`。所有 `InjectorBase` 子类通过反射自动发现并注册为 Singleton。最后调用 `Extensions.BuildServiceProvider(services)` 构建全局容器。

WPF 资源字典 `GitButtonStyle.xaml` 和 `Icons.xaml` 在初始化时合并到 `Application.Current.Resources`。

窗口事件（`WindowWatcher.WindowCreated` / `WindowActivated`）触发 `ScheduleProcess`，遍历所有 `InjectorBase` 子类，对匹配的窗口调用 `CanInject` → `InjectAsync`。

## Injector 架构

### 抽象基类

所有 UI 注入逻辑继承 `InjectorBase`（`Injectors/InjectorBase.cs`），实现 `CanInject(string caption)` 和 `InjectAsync(string caption, CancellationToken)` 两个抽象方法。新增 UI 注入只需新增一个 `InjectorBase` 子类，无需修改 `GitPlusPackage`。

### GitWindowActionButtonPanelInjector

`Injectors/GitWindowActionButtonPanelInjector.cs` — 在 Git 窗口按钮面板中注入"拉取(自动储藏)"按钮。通过 `[RequiredArgsConstructor]` 注入 `ILogger` 和 `GitCommandService`。注入流程：定位按钮面板 → 复制原有 `pullButton` 创建 `pullWithStash` 按钮（同步本地值、图标、样式）→ 插入面板。操作时使用 `GitWindowViewModelExtensions` 在 VS 内置 InfoBar 显示进度和结果。

## GitWindowViewModelExtensions（反射扩展）

`Commons/GitWindowViewModelExtensions.cs` — 通过反射调用 VS 内部 `GitWindowViewModel` 的方法（`ShowNotification`、`ShowError`、`ShowException`、`ClearNotifications`），在 VS 原生通知栏中显示消息。方法签名通过反射匹配，参数使用 `Enum.ToObject` 转换 VS 内部枚举类型。所有调用有 try-catch 保护。

## GitWindowLocator（静态工具类）

`Commons/GitWindowLocator.cs` 是 `static class`，**不注册 DI**。通过 `LocateGitButtonPanelAsync()` / `LocateGitCommentTextBoxAsync()` 静态方法，内部使用 `FindChildAsync` 按 Name 定位 `gitWindowMainGrid` → `buttonPanel` / `commentTextBox`。

## 配置

### DialogPage + record POCO 分离

`GitPlusOptionPage`（`DialogPage`）包含 `[Category]` / `[DisplayName]` / `[Description]` 注解的属性：`TimeoutSeconds`、`GitFilePath`、`AutoFetchEnabled`、`AutoFetchIntervalMinutes`、`UseRebase`、`LogLevel`。通过 `ToOption()` 转换为轻量 `record GitPlusOption`，注入 DI 供服务使用。服务通过 `Extensions.GetRequiredService<GitPlusOption>()` 获取配置实时快照。

## WPF VisualTree 操作

相关扩展方法定义在 `Commons/Extensions.cs`。

- **`FindChildAsync(name, recursive, cancellationToken)`** — 按 Name 定位子元素，递归搜索 VisualTree
- **`GetChildIndexAsync(elementName, cancellationToken)`** — 获取子元素名称及索引，支持 ToolBarTray / ItemsControl / Panel
- **`CopyLocalValuesFrom(source)`** — 同步复制本地值，跳过 `ReadOnly`、`Name` 和 `Expression` 值，并同步 `DataContext`、`Style`、`Resources`
- **`InsertElementAsync(element, index, cancellationToken)`** — 插入元素，自动检查重复，支持 ToolBarTray / ItemsControl / Panel
- **`RemoveElement(element)`** — 按容器类型移除元素

## Git 操作

### 进程调用

`Services/GitCommandService.cs` — 通过 `git.exe` 进程执行 Git 命令，超时时间从 `GitPlusOption.TimeoutSeconds` 读取（默认 30 秒）。标准输出和标准错误并发读取防止死锁。对外暴露：`FetchAsync`、`PullAsync`（可选 `--rebase`）、`PushAsync`、`StatusAsync`、`StashPushAsync`、`StashPopAsync`。

### 结果模型

`Commons/GitResult.cs` — `GitResult` 通过工厂方法构建（`Success` / `Failure`），永不直接 `new`。包含属性 `IsSuccess`、`Output`、`Error`、`Exception`。

## 按钮样式

### ImageButton + GitButtonStyle

使用 VS SDK 内置 `ImageButton`（`Microsoft.VisualStudio.PlatformUI.ImageButton`）配合 `Resources/GitButtonStyle.xaml` 中的 `GitButtonStyle` 样式。按钮创建时设置 `Name`、`Style`、三态图标（`ImageNormal` / `ImageHover` / `ImagePressed`）、`Command`（`AsyncRelayCommand`）和 `ToolTip`。

`GitButtonStyle` 为 `ControlTemplate`，内部 `Border`（24×24）+ `Image`（`PART_Image`），通过 Trigger 绑定 VS 主题色。

### 图标

`Resources/Icons.xaml` — 所有图标用 `DrawingImage` + `GeometryDrawing` 定义，每种图标提供 `Normal` / `Hover` / `Pressed` 三态，分别绑定 `CommandBarTextActiveBrushKey` / `CommandBarTextHoverBrushKey` / `CommandBarTextSelectedBrushKey`。

## 日志输出

`Commons/OutputWindowLogger.cs` — 实现 `ILogger`，输出到 VS Output Window "Git +" 窗格。`IsEnabled` 根据 `GitPlusOption.LogLevel` 过滤。通过 DI 解析 `IVsOutputWindow` 创建窗格，线程安全（非 UI 线程自动 marshal 到主线程）。使用 `FileAndForget("gitPlus/...")` 确保异步写操作不被遗忘。


| 日志          | 用途       | 示例                        |
| ----------- | -------- | ------------------------- |
| Trace       | 方法执行流程   | Enter/Exit、耗时、Git 命令、反射调用 |
| Debug       | 调试信息     | 找到按钮、注入成功、获取仓库、按钮状态       |
| Information | 用户关注的事件  | 当前执行的 git 命令      |
| Warning     | 可恢复异常    | Git 未安装、当前没有仓库、按钮不存在      |
| Error       | 操作失败     | Git Pull 失败、反射异常          |
| Critical    | 插件无法继续工作 | Package 初始化失败、核心服务无法创建    |

> Trace 和 Debug 类型的日志格式 [ClassName] message

## 测试

`GitPlus.Tests/` — **xunit** + **Moq**，目标 `net472`。测试引用 WPF 程序集（`PresentationFramework` / `PresentationCore` / `WindowsBase`）以支持 `DependencyObject` 测试。

## 不应出现的模式

| 应避免 | 应使用 |
|--------|--------|
| 为服务创建 `ISomething` 接口 | 直接用具体类，`sealed partial class` |
| 自定义 `ILogger` 接口 | `Microsoft.Extensions.Logging.ILogger` |
| 在 csproj 逐个引用 WPF 程序集 | `<UseWPF>true</UseWPF>` |
| 手动写构造函数 | `[RequiredArgsConstructor]` + `sealed partial` |
| 按类型名或索引定位 WPF 元素 | `FindChildAsync(name)` 按 Name 定位 |
| 直接用 `new` 构造 DialogPage | 通过 `GetDialogPage(typeof(GitPlusOptionPage))` |
| `await TaskScheduler.Default` | `JoinableTaskFactory.SwitchToMainThreadAsync()` |
| `Debug.WriteLine()` | 通过 `ILogger` 写 Output Window |
| 自定义 Button 控件 | 直接用 `ImageButton` + `GitButtonStyle` |
| 手动创建 `ServiceRegistration` 扩展类 | DI 注册在 `InitializeAsync` 中内联完成 |
| 将 GitWindowLocator 注册为 DI 服务 | 静态工具类，直接通过 `GitWindowLocator.LocateXxxAsync()` 调用 |
| 在 GitPlusPackage 中直接写 UI 注入逻辑 | 继承 `InjectorBase`，通过反射自动发现注册 |
