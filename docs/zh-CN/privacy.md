# 数据与隐私

BrimDeck 仅读取应用已有的登录状态与用量记录，不会修改、续期或注销它们的登录，也不会上传会话内容或收集使用数据。剪贴板记录、文件中转、系统监控和设备电量的数据都只在本机使用。

## 读取的数据

| 工具 | 配额 | Token 用量与费用 |
| --- | --- | --- |
| Claude | 读取 Claude Code 的登录（`%USERPROFILE%\.claude\.credentials.json`），不可用时改用 Claude 桌面版的登录，向 Anthropic 查询账户用量。账户同时属于多个组织（例如个人 Pro 与 Team）时，用桌面版为各组织保存的登录分别查询。每一行可以单独关闭"联网获取额度"：关闭的行不读取登录、不联网，只显示 Claude 桌面版在本机记录的额度（`plan-usage-history.json`） | 读取 Claude Code 的本地会话记录（`projects\**\*.jsonl`），不含 Claude 桌面版的普通聊天 |
| Codex | 读取 `%USERPROFILE%\.codex\auth.json` 中的登录，向 OpenAI 查询账户用量；查询失败时显示会话记录中的配额，并标明可能已过时 | 读取本地会话记录（`sessions` 与 `archived_sessions`） |
| Antigravity | 从正在运行的 Antigravity 桌面版的本机服务（仅限 127.0.0.1）读取 | 读取 `%USERPROFILE%\.gemini` 下桌面版与 CLI 的本地会话数据库 |
| Cursor | 以只读方式从 `%APPDATA%\Cursor\User\globalStorage\state.vscdb` 读取登录，向 Cursor 查询账户用量 | 向 Cursor 查询账户用量明细，包含该账户在其他设备上的用量 |
| Grok Bot | 读取 Grok Bot 桌面版自己的登录（`%APPDATA%\Grok Bot\sand-secrets.json`），向 Cursor 查询该账户的每周额度。每一行可以单独关闭"联网获取额度"：关闭的行不读取登录、不联网，只显示 Grok Bot 桌面版在本机缓存的每周额度（`%APPDATA%\Grok Bot\sand-client-persistence`） | 不提供 |
| ZCode | 读取 ZCode 的登录，查询 Coding Plan、Start Plan 与 MCP 每日额度 | 读取本地请求记录（`%USERPROFILE%\.zcode\cli\db\db.sqlite`） |
| DeepSeek Harness | 读取 DeepSeek Harness 的登录（`%USERPROFILE%\.dsh\.credentials.yaml`），向 DeepSeek 开放平台查询账户余额与累计消费金额；未登录账户时，使用其中保存的 API 密钥查询余额 | 读取本地会话记录（`%USERPROFILE%\.dsh\sessions`） |
| Kimi Code | 读取 Kimi Code 的登录（`%USERPROFILE%\.kimi-code\credentials\` 下的 `kimi-code.json`，海外版为 `kimi-code-env-…json`）、服务地址与站点设置（`config.toml`，以及可选的 `region` 文件），向所选站点（api.kimi.com 或 api.kimi.ai）查询套餐额度与会员等级；不续期登录，不修改 Kimi Code 的任何文件 | 读取本地会话记录（`%USERPROFILE%\.kimi-code\sessions`） |
| OpenCode | 以只读方式读取 OpenCode 数据库（`%USERPROFILE%\.local\share\opencode\opencode.db`）中的 OpenCode Console 登录，没有时读取同一目录下 `auth.json` 中的 API 密钥，向 opencode.ai 查询 OpenCode Go 额度；不续期登录，不修改 OpenCode 的任何文件 | 以只读方式读取同一数据库中的模型请求记录 |
| 智谱 GLM、Z.ai GLM、DeepSeek、Kimi Code API、OpenCode Go、OpenRouter、New API、Sub2API、自定义 | 使用在设置中填写的密钥与站点地址；Kimi Code API 的密钥只发往 api.kimi.com 与 api.kimi.ai，读取的额度不写入 `kimi-quota.json`；OpenCode Go 的密钥只发往 opencode.ai；OpenRouter 的密钥只发往 openrouter.ai。密钥只通过 HTTPS 或发往本机地址；New API、Sub2API、自定义来源打开“允许 HTTP”后，也会通过 HTTP 以明文发送 | 不提供 |

- 设置了 `CLAUDE_CONFIG_DIR`、`CODEX_HOME`、`ZCODE_HOME`、`DSH_HOME` 或 `KIMI_CODE_HOME`（须为绝对路径）时，从该目录读取。OpenCode 按 `XDG_DATA_HOME` 与 `OPENCODE_DB` 确定数据库位置。
- 这些账户接口与本地数据格式并非公开的稳定接口。无法读取时，面板显示实际状态，不会显示虚构的数值。

## 本机保存的文件

所有文件保存在 `%LOCALAPPDATA%\BrimDeck`：

| 文件 | 内容 |
| --- | --- |
| `settings.json` | 设置，不含任何登录令牌或 API 密钥 |
| `secrets.dat` | 手动填写的 API 密钥，使用 Windows 的加密功能按当前用户加密 |
| `model-prices.json`、`manual-model-prices.json` | 同步的模型单价、手动添加的单价 |
| `official-prices.json` | 从 OpenRouter 同步的模型厂商官方渠道单价，只包含记录中用到的模型，以及 Kimi Code 计费所需的 Kimi K3、K3.1 Pro 与 K3.1 Pro Highspeed。没有这个文件时，读取旧版本保存的 `moonshot-prices.json` |
| `claude-quota.json` | 最近一次联网读取的 Claude 额度（各项百分比、重置时间与订阅名称），重启后接口被限流时用来继续显示 Fable 等额度；不含登录令牌；账户与组织仅以其 ID 的单向哈希记录（不保存 ID 本身），每个组织一条，只在同一账户登录时使用；超过 7 天不再使用，登录失效或退出登录时删除 |
| `kimi-quota.json` | 最近一次联网读取的 Kimi Code 额度（各项百分比、重置时间、加油包与会员等级），登录过期或接口暂时不可用时继续显示，重启后同样适用；不含登录令牌与其他账户资料；账户仅以其 ID 连同服务地址的单向哈希记录（不保存 ID 本身），只在同一站点的同一账户登录时使用；超过 7 天不再使用，退出登录或账户未订阅 Kimi Code 套餐时删除 |
| `update-prompt.txt`、`updates\` | 已提示过的版本号、下载的安装包或免安装压缩包（免安装版更新时还有解压出的新版本文件） |
| `cursor-prices.json` | Cursor 合并套餐每周已含用量的读数（金额与各类 Token 数），用于估算该套餐下的费用 |
| `apple-music-lyrics.json` | Apple Music 中每首歌匹配到的歌词编号，重启后再次播放时直接使用 |
| `shelf.dat`、`shelf\` | 剪贴板记录、文件中转的文件路径和两者的分类，以及复制的图片（每张一个文件），都使用 Windows 的加密功能按当前用户加密。普通的剪贴板记录在退出 BrimDeck 时清除（异常退出的，在下次启动时清除）；“常用”和一直保留的分类中的内容保留到手动删除 |
| `themes\` | 自定义的面板主题文件 |
| `sensor-stops.json`、`sensor-pending.json` | 系统监控因电脑卡住而停止读取的项目（硬盘或显卡的型号、主板型号与 BIOS 版本），以及正在读取的项目，用于在下次启动时发现上次卡住的读取；在设置中重新开启后删除对应记录 |
| `crash.log` | 错误记录，超过 1 MB 时清空 |

## 联网与安全

- 仅连接以下地址：各工具的官方接口、你在设置中填写的站点、OpenRouter 模型目录及记录中用到的模型的渠道列表（同步模型单价，只发送模型名称）、歌词服务（LRCLIB、网易云音乐、QQ 音乐、酷狗音乐）、播放器提供的封面地址，以及 GitHub Releases（检查更新；启用增强传感器时下载传感器服务与 PawnIO 官方模块）。
- Claude 桌面版的数据目录包括 `%APPDATA%\Claude`、商店版 `%LOCALAPPDATA%\Packages\Claude_*\LocalCache\Roaming\Claude` 和 `%LOCALAPPDATA%\Packages\AnthropicPBC.Claude_*\LocalCache\Roaming\Claude`，以及 `%LOCALAPPDATA%\Claude-Data\<安装名>`。
- 读取的登录令牌仅在内存中使用，不写入磁盘；Claude 桌面版与 Grok Bot 桌面版的登录缓存使用当前用户的 Windows 加密功能在内存中解密。
- 自定义脚本在沙盒中运行，无法访问文件、进程或 .NET 对象，限制详见[支持的工具](ai-tools.md#自定义脚本)。
- 系统监控的读数（包括温度和硬盘空间）只在本机读取和显示，不保存、不上传。硬盘空间来自各盘符的文件系统，盘符所在的物理硬盘通过 Windows 的卷信息查询。温度默认通过 Windows 自带的接口读取：CPU 用性能计数器中的 ACPI 热区，GPU 用显卡驱动报告的数据（显卡内核接口没有时，改用 NVIDIA、AMD、Intel 驱动自带的接口），硬盘以不可读写的方式打开后查询其温度；不需要管理员权限，不安装驱动。如果正在运行 HWiNFO（已开启共享内存）或 LibreHardwareMonitor（已开启远程网页服务，只连接本机 localhost:8085），也会从中读取 CPU 温度和功耗。启用[增强传感器](enhanced-sensors.md)后，CPU 温度和功耗由 BrimDeck 的传感器服务通过 PawnIO 驱动从处理器读取。
- 整机功耗和本机电池同样只在本机读取和显示，不保存、不上传，且只在系统监控页或收起行选了该读数时读取：CPU 功率来自 Windows 能量计（Energy Meter）的性能计数器，NVIDIA 显卡功率来自显卡驱动自带的 nvidia-smi.exe（只在显卡有负载、且页面或收起行要显示功耗时由 BrimDeck 启动，随 BrimDeck 退出而结束；显卡空闲时不读取，以免唤醒休眠的独立显卡），电池电量、充放电功率和容量来自 Windows 的电池接口，电源模式和节电模式来自 Windows 的电源接口；不需要管理员权限，不安装驱动。
- 帧率默认不读取。开启“显示帧率”时，需要把当前用户加入 Windows 的“Performance Log Users”组，Windows 会请求一次管理员确认；之后只读取前台程序绘制画面的事件，用来计算帧率并显示程序名称，不保存、不上传。
- 设备电量只在设备电量页、收起行的“设备电量”或“设置 › 显示与设备”显示时读取，不保存、不上传，也不联网。蓝牙设备的电量来自 Windows 自己读取的数值（与“Windows 设置 › 蓝牙和其他设备”一致），必要时读取设备的蓝牙电池服务；使用 2.4G 接收器或数据线的设备通过其 HID 接口发送只读查询；Xbox 手柄通过 Windows 游戏控制器接口；AirPods 与 Beats 只在已连接时从近距离的蓝牙广播中读取。支持的品牌见[设备电量支持](devices.md)。不会配对、断开或改动任何设备与 Windows 设置，不需要管理员权限，不安装驱动。在设置中调整过顺序、名称、颜色或显示位置的设备记在 `settings.json` 里（设备标识、名称、类型、自定义名称、颜色与显示位置）。
- 剪贴板记录只在“记录剪贴板”开启时读取系统剪贴板中的文字和图片，连同复制它的应用的进程名一起保存在本机，不上传。“不记录的应用”中的应用，以及密码管理器等应用声明不希望被剪贴板历史保存的内容，不会记录。文件中转只记录文件的路径，并读取文件的图标、大小和修改时间用于显示，不读取文件内容，也不复制文件。详见[剪贴板与文件中转](clipboard.md)。
- 律动条所用的声音仅在内存中分析，不会保存。

## 剪贴板临时图片

新配置默认关闭“记录剪贴板”，开启后才收集内容。拖出图片时，接收应用需要明文 PNG，临时文件位于 `%TEMP%\BrimDeck\Drag`。启动和正常退出时清理这些 PNG，每次拖动也清理超过十分钟的文件。异常退出后的文件在下次启动清理；尚未再次启动时，不保证文件立即消失，也不承诺安全擦除。

## 更新签名

自动安装更新需要通过应用内嵌可信公钥的 ECDSA P-256 签名校验，并验证包的大小和 SHA-256。公钥不从发布源下载；没有配置可信公钥的构建只提供手动下载。签名私钥不应进入源码或构建产物，见[构建与签名配置](build.md)。

## 网易云音乐完整控制

本 fork 不再启动网易云的调试模式或写入新的 DLL 补丁；可使用网易云的 SMTC 系统媒体控制。旧补丁可在“设置 › 播放器 › 网易云音乐”关闭“旧版调试补丁”，之后需要完全退出并重新启动网易云。还原受保护目录中的文件可能需要管理员确认。

旧版本或其他软件开启的 `127.0.0.1:21631` 调试端口允许本机程序控制播放器、读取页面和登录状态。仅退出 BrimDeck 不会关闭已经运行的播放器端口。本 fork 可以读取已由其他工具打开的本机播放器连接，但不会代为开启端口。

## 酷狗音乐完整控制

本 fork 不再修改酷狗 DLL 来开启调试端口，标准系统媒体控制继续提供曲目、播放、暂停和切歌。历史补丁通过“设置 › 播放器 › 酷狗音乐”的“旧版调试补丁”关闭，还原后完全退出并重新启动酷狗。

旧补丁打开的 `127.0.0.1:12233` 具有同类调试权限。安装本 fork 本身不会关闭其他程序已经打开的端口。
