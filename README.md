# DeepSeekBalance

DeepSeek API 余额及用量监控桌面应用，基于 Avalonia 12 + .NET 10，支持 **Windows** 和 **macOS**。


## 功能

| 功能 | 说明 |
|------|------|
| **余额查询** | DeepSeek 官方 `/user/balance` 接口，显示 CNY/USD 余额、赠金、充值 |
| **用量统计** | Token 消耗量、API 请求次数、缓存命中率（V4 Flash / V4 Pro） |
| **消费趋势** | 7 天堆叠柱状图，显示缓存命中/未命中/输出 Token |
| **自动刷新** | 1 分钟 / 5 分钟 / 30 分钟 / 1 小时 可选周期 |
| **亮/暗主题** | 一键切换，配色基于 GitHub Dark/Light |
| **跨平台** | Windows x64 + macOS arm64/x64（AOT 编译） |

## 界面截图

三页面架构：

```
Dashboard → Settings → Model Detail
   ↑___________↓
```

### 主面板
- 余额卡片：大号金额 + 状态指示 + 当日/本月消费
- 模型卡片：V4 Flash / V4 Pro，Token 进度条 + 缓存命中率 + 费用
- 图表：7 天堆叠柱状图（绿色命中/红色未命中/蓝色输出）

### 设置页
- API Key 管理（保存/清除）
- Usage Token 管理（手动粘贴或浏览器获取）
- 自动刷新开关及间隔选择

## 技术栈

| 组件 | 版本 |
|------|------|
| .NET | 10.0 |
| Avalonia UI | 12.0 |
| CommunityToolkit.Mvvm | 8.4 |
| System.Text.Json | 源码生成（AOT 兼容） |

## 获取 Usage Token

Usage Token 与 API Key 不同，用于查询 DeepSeek 平台用量数据（非官方公开 API）：

1. 浏览器登录 [platform.deepseek.com](https://platform.deepseek.com)
2. 按 `F12` 打开开发者工具
3. 在 Console 输入：`JSON.parse(localStorage.userToken).value`
4. 复制返回值，粘贴到应用设置页

## 构建与发布

```bash
# 开发构建
dotnet build

# 发布 macOS (Apple Silicon) — Native AOT
dotnet publish -c Release -r osx-arm64 -p:PublishSingleFile=true --self-contained

# 发布 Windows x64 — 裁剪优化
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained -p:PublishAot=false
```

产物输出至 `out/<runtime-identifier>/`。

### 体积对比

| 平台 | 编译模式 | 可执行文件 | 总大小 |
|------|---------|-----------|--------|
| macOS arm64 | Native AOT | 20MB | 38MB |
| Windows x64 | Trimmed | 22MB | 40MB |

## 项目结构

```
DeepSeekBalance/
├── Assets/             # 应用图标 (.svg/.ico/.icns)
├── Models/             # API 响应数据模型
├── Services/           # API 调用 + 配置管理
├── ViewModels/         # MVVM ViewModel
├── Views/              # Avalonia XAML 界面
├── App.axaml(.cs)      # 应用入口 + 主题/DI配置
├── Program.cs          # 启动入口
├── Info.plist          # macOS 应用清单
└── DeepSeekBalance.csproj
```

## 许可证

MIT License
