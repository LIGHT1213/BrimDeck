# 在 GitHub 上构建 Windows exe

需要 GitHub CLI（`gh`），并登录拥有仓库访问权限的账号。fork 首次构建前，打开自己仓库的 **Actions** 页，启用工作流。

在本仓库的 clone 目录运行：

```powershell
gh workflow run build-windows.yml --ref main
gh run list --workflow build-windows.yml --limit 5
# 将下面的 RUN_ID 替换成上一条命令输出的运行编号。
gh run watch RUN_ID --exit-status
gh run download RUN_ID --dir artifacts/download
```

`Build Windows executables` 工作流在 Windows runner 上构建 .NET 10 / WPF 程序，运行核心、媒体、Windows hosted 子集，以及安装更新、卸载、免安装更新和回滚测试。全部通过后上传构建产物，保留 30 天：

- `BrimDeck-<版本>-Setup.exe`：自包含安装程序，无需单独安装 .NET。
- `BrimDeck-<版本>-Portable-x64.zip`：解压后运行 `BrimDeck/BrimDeck.exe`，保留同目录的所有文件。
- `BrimDeck.Sensors.exe`、`update.json`、`SHA256SUMS.txt`。

版本由 `releases/` 中最新版本的说明文件决定，可能是 alpha 或 beta。本工作流只生成 Actions 构建产物，不发布 Release。构建产物中的安装版和免安装版均附带传感器程序，因此启用增强传感器不依赖同名 GitHub Release；附带的程序仍按构建时写入的 SHA-256 校验。

GitHub Actions 构建会将当前仓库名写入程序，更新、传感器下载和主页使用该仓库，不会把 fork 更新回上游。只生成 Actions 产物的仓库尚无更新源时，自动更新检查会报告暂时不可用；以后发布正式 Release 后即可提供更新。

本地 Windows 打包需要 .NET 10 SDK，并显式指定 fork：

```powershell
./scripts/Publish-Release.ps1 -Version 1.3.1-alpha.1 -Repository LIGHT1213/BrimDeck -BundleSensors
```

发布 Release 时省略 `-BundleSensors`，传感器程序会按原流程作为独立 Release 文件下载和校验。上游作者与许可证信息保持在原项目及安装程序中。
