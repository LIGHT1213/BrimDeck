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

## 本次修复的上传与运行

本次修复已推送到 `LIGHT1213/BrimDeck` 的 `main`，Windows 构建与验证已通过，结果见[代码复审与修复记录](security-review.md)。已有本地 clone 可执行 `git pull --ff-only origin main` 获取修复，再按上面的命令重新构建。

GitHub 集成返回 `Resource not accessible by integration` / 403 时，需在 ChatGPT 的 GitHub 连接中把 `LIGHT1213/BrimDeck` 加入授权仓库，授予 Contents 与 Workflows 写入及 Actions 运行权限。

也可以在你自己的终端进入含本次修复的仓库后，使用 GitHub CLI 的网页登录：

```powershell
gh auth login --hostname github.com --git-protocol https --web
gh auth refresh --hostname github.com --scopes repo,workflow
gh auth setup-git
git push origin main
gh workflow run build-windows.yml --repo LIGHT1213/BrimDeck --ref main
gh run list --repo LIGHT1213/BrimDeck --workflow build-windows.yml --limit 5
gh run watch RUN_ID --repo LIGHT1213/BrimDeck --exit-status
gh run download RUN_ID --repo LIGHT1213/BrimDeck --dir artifacts/download
```

如果 `gh` 提示认证由 `GH_TOKEN` 或 `GITHUB_TOKEN` 管理，先在当前终端移除旧的只读环境变量，再网页登录；不要在聊天里发送令牌。PowerShell 可运行 `Remove-Item Env:GH_TOKEN, Env:GITHUB_TOKEN -ErrorAction SilentlyContinue`；Bash 可运行 `unset GH_TOKEN GITHUB_TOKEN`。

无需配置签名密钥即可生成、安装和运行 exe；没有可信公钥时，应用只支持手动更新。

## 可选：配置签名自动更新

此 fork 使用 P-256 私钥签名 `update.json`，应用只信任编译时嵌入的 SPKI 公钥。公钥不从更新服务器下载。安装程序本身尚无 Authenticode 代码签名，因此 Windows 仍可能显示未知发布者。

在本机 .NET 10 环境生成专用密钥，路径必须在仓库之外，文件不存在且其父目录已经存在：

```powershell
dotnet run --project scripts/update-signing -c Release -- keygen <仓库外的私钥文件绝对路径>
```

命令只打印公钥，私钥写入指定文件。将打印的 SPKI base64 设置为仓库 Actions Variable `UPDATE_SIGNING_PUBLIC_KEY`。正式发布时，可把 PEM 私钥设置为受审批保护的 `release` 环境 Secret `UPDATE_SIGNING_PRIVATE_KEY`；只有发布工作流会读取该私钥，手工 exe 构建不需要它。不要将私钥提交到 Git 或构建产物。

更严格的分离方式是在可信本机离线签名：将私钥加载到当前进程的 `BRIMDECK_UPDATE_SIGNING_KEY`，公钥设置为 `BRIMDECK_UPDATE_PUBLIC_KEY`，然后运行：

```powershell
dotnet run --project scripts/update-signing -c Release -- sign <update.json的路径>
dotnet run --project scripts/update-signing -c Release -- verify <update.json的路径> <SPKI公钥>
```

签名完成后清除私钥环境变量；更新 `SHA256SUMS.txt` 中清单文件的哈希。公钥变更必须通过已有可信签名授权的新版本或手工可信安装分发。发布权限与签名私钥同时失陷仍会破坏这条信任链。
