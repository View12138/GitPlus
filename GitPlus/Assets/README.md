# Assets

扩展的静态资源文件，包括本地化、图标、截图及 Conventional Commits 规则定义。

## 图标

Logo 及截图均存放于此。除 `Logo.png` 外，所有截图命名为 `Screenshots*` 并自动嵌套在 `Logo.png` 下。

## 本地化

`Languages.resx` 为英文基准资源，其他语言(如 `Languages.zh-Hans.resx`)及自动生成的 `Languages.Designer.cs` 均自动嵌套在其下。

**使用规范**：用户可直接感知的 UI 文本（按钮、标签、提示等）必须使用本地化字符串；日志、异常消息、调试输出等面向开发者的文本不使用本地化。

## 约定式提交

> 提交规则定义遵循 [Conventional Commits](https://www.conventionalcommits.org/) [[Github](https://github.com/conventional-commits/conventionalcommits.org)] 规范, 

提交规则以 `conventional-commits-rules.json` 为英文基准规则，其它语言（如 `conventional-commits-rules.zh-Hans.json`）均自动嵌套在其下。
