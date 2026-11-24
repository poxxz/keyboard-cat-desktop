# 键盘小黑猫

本 fork 新增四只猫：金色眼睛的星期三、简州虎斑白猫咖啡、英短银渐层、暹罗猫（在托盘菜单“猫咪样式”里切换）。

**[打开体验入口](https://keyboard-cat.app.workbuddy.host/?sharecode=6OHA4TQODE&shareId=8b833b2c65444fb4953ce4940ffc3a2e)** · [下载 Windows 和 macOS 安装包](https://github.com/liangcyprus855-ship-it/keyboard-cat-desktop/releases/latest)

右键小猫，打开「小猫大小」即可在 1% 到 200% 之间拖动缩放。也可以从 Windows 系统托盘或 Mac 菜单栏打开这个设置。

## 从源码构建

请在仓库根目录运行对应命令。

| 平台 | 准备 | 构建命令 |
| --- | --- | --- |
| Windows 10 / 11 | .NET Framework 4.8 和 NSIS 3 | `powershell -File .\windows\build.ps1` |
| macOS | Xcode 命令行工具 | `bash macos/build.sh` |

[Windows 源码](windows/) · [macOS 源码](macos/) · [美术资源导出工具](art-tools/) · [MIT 许可证](LICENSE)
