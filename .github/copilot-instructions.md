# GitPlus — VS Extension 开发约定

> 本指南只展示正确做法，不包含反例代码。用肯定句式描述。反模式对照见文末。

## 技术栈

- `net472` / LangVersion 14 / `<UseWPF>true</UseWPF>` / `<Nullable>enable</Nullable>`
- **NuGet**：`Microsoft.VisualStudio.SDK`（ExcludeAssets=runtime）、`Microsoft.VSSDK.BuildTools`、`CommunityToolkit.Mvvm`、`Lombok.NET`、`Microsoft.Extensions.DependencyInjection`、`Microsoft.Extensions.Logging.Abstractions`
- **抑制警告**：`VSTHRD010 VSTHRD200 VSSDK007 VSTHRD002 VSTHRD001 VSTHRD103 CS8618`

## 命名规范

- **禁止下划线前缀**：字段/属性/变量/方法/类名不用 `_` 开头。可用下划线分隔单词（如 `some_field_name`）。例外：XAML `PART_` 前缀
- **变量名全拼**：避免缩写。例外：`Id`、`Xml`、`Http`、`Uri`、`Dto`、`DTE` 等行业通用缩写

## 项目结构

| 目录 | 说明 |
|------|------|
| `GitPlusPackage.cs` | AsyncPackage 入口，DI 初始化 + 窗口事件绑定 + Injector 调度 |
| `Configurations/` | `GitPlusOptionPage`（DialogPage）+ `GitPlusOption` record |
| `Injectors/` | `InjectorBase` 抽象基类 + 各 UI 注入子类 |
| `Services/` | `WindowWatcher`、`GitCommandService`、`AutoFetchService` |
| `Commons/` | `Extensions`（DI/WPF/String）、`GitResult`、`GitWindowLocator`（static）、`GitWindowViewModelExtensions`（反射）、`OutputWindowLogger` |
| `Resources/` | `GitButtonStyle.xaml`、`Icons.xaml` |
| `Assets/` | `Logo.png`、`Languages.resx`、`Languages.zh-Hans.resx`、`Languages.Designer.cs` |
| `Properties/` | `GlobalUsings.cs`、`IsExternalInit.cs` |
| `GitPlus.Tests/` | xunit + Moq，`net472` |

## 架构要点

### DI

- **无接口**：服务直接用具体类注册，`sealed partial class`
- **内联注册**：`InitializeAsync` 中直接 `new ServiceCollection().AddSingleton<T>()...`，不用独立扩展方法
- **全局容器**：`Extensions.BuildServiceProvider(services)` 静态方法构建，之后通过 `Extensions.GetRequiredService<T>()` 访问

### Lombok.NET

- 用 `[RequiredArgsConstructor]` + `sealed partial`，`readonly` 字段自动纳入构造函数

### 日志

- 统一 `ILogger`（`Microsoft.Extensions.Logging`），输出到 VS Output Window "Git +" 窗格
- 线程安全：非 UI 线程自动 `SwitchToMainThread` + `FileAndForget`

### GlobalUsings

- `Properties/GlobalUsings.cs` 集中管理，已涵盖 system、microsoft extensions、CommunityToolkit.Mvvm、`GitPlus.*`、Lombok.NET

## Package 初始化流程

1. 创建 `OutputWindowLogger`
2. `ServiceCollection` 注册 `ILogger`、`WindowWatcher`、`GitCommandService`、`AutoFetchService`
3. `AddTransient` 注入 `GitPlusOption`（`GetDialogPage`）、`DTE`（`GetGlobalService`）、`IVsOutputWindow`（`GetGlobalService`）
4. 反射发现所有 `InjectorBase` 子类 → `AddSingleton`
5. `Extensions.BuildServiceProvider(services)`
6. 合并 WPF 资源字典（`GitButtonStyle.xaml`、`Icons.xaml`）
7. 绑定 `WindowWatcher` 事件 → `ScheduleProcess` → 遍历 Injector 执行注入
8. 启动 `WindowWatcher`、`AutoFetchService`

## Injector 架构

- `InjectorBase`：`CanInject(string caption)` + `InjectAsync(string caption, CancellationToken)`
- 新增 UI 注入仅需继承 `InjectorBase`，反射自动发现，无需改 `GitPlusPackage`
- `GitWindowActionButtonPanelInjector`：复制 `pullButton` → 创建 `pullWithStash` 按钮 → 插入面板，用 `GitWindowViewModelExtensions` 显示 InfoBar 进度

## 关键类

- **`GitWindowLocator`**（static）：`LocateGitButtonPanelAsync()` / `LocateGitCommentTextBoxAsync()`，按 Name 定位 VisualTree
- **`GitWindowViewModelExtensions`**（扩展方法，反射）：`ShowNotification` / `ShowError` / `ShowException` / `ClearNotifications`，调用 VS 内部 ViewModel，有 try-catch 保护
- **`GitResult`**：工厂方法 `Success(output)` / `Failure(error, exception?)`，永不 `new`
- **`GitPlusOption`**（record）：`TimeoutSeconds`、`GitFilePath`、`AutoFetchEnabled`、`AutoFetchIntervalMinutes`、`UseRebase`、`LogLevel`

## WPF VisualTree 操作

扩展方法位于 `Extensions.cs`：

- `FindChildAsync(name, recursive, ct)` — 按 Name 定位
- `GetChildIndexAsync(elementName, ct)` — 获取索引（支持 ToolBarTray/ItemsControl/Panel）
- `CopyLocalValuesFrom(source)` — 同步复制本地值，跳过 ReadOnly/Name/Expression
- `InsertElementAsync(element, index, ct)` — 插入（自动查重，支持 ToolBarTray/ItemsControl/Panel）
- `RemoveElement(element)` — 按容器类型移除

## Git 操作

- `GitCommandService`：`FetchAsync` / `PullAsync(rebase?)` / `PushAsync` / `StatusAsync` / `StashPushAsync` / `StashPopAsync`
- 超时从 `GitPlusOption.TimeoutSeconds` 读取，stdout/stderr 并发读取防死锁

## 按钮样式

- 用 VS SDK `ImageButton` + `GitButtonStyle`（ControlTemplate，24×24 Border + `PART_Image`），Trigger 绑定 VS 主题色
- 图标：`DrawingImage` + `GeometryDrawing`，三态（Normal/Hover/Pressed），绑定 `CommandBarTextActiveBrushKey` 等

## 本地化

- 仅改 `Assets/*.resx`，不改 `Languages.Designer.cs`
- 代码中应使用 `nameof(Assets.Languages.*)` 或 `Assets.Languages.*` 来引用，而不是硬编码

## 测试

- MSTest，`net472`，引用 `PresentationFramework`/`PresentationCore`/`WindowsBase`

## 不应出现的模式

| 应避免 | 应使用 |
|--------|--------|
| 为服务创建接口 | 直接用具体类 |
| 自定义日志接口 | `Microsoft.Extensions.Logging.ILogger` |
| 逐个引用 WPF 程序集 | `<UseWPF>true</UseWPF>` |
| 手动写构造函数 | `[RequiredArgsConstructor]` + `sealed partial` |
| 按类型名/索引定位元素 | `FindChildAsync(name)` |
| `new` DialogPage | `GetDialogPage(typeof(...))` |
| `await TaskScheduler.Default` | `JoinableTaskFactory.SwitchToMainThreadAsync()` |
| `Debug.WriteLine()` | `ILogger` 写 Output Window |
| 自定义 Button 控件 | `ImageButton` + `GitButtonStyle` |
| 独立 ServiceRegistration 扩展类 | `InitializeAsync` 中内联注册 |
| GitWindowLocator 注册到 DI | static class，直接调用 |
| 在 Package 中直接写 UI 注入 | 继承 `InjectorBase`，反射自动发现 |
