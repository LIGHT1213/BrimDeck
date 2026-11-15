namespace BrimDeck.Core;

// Standard media controls remain available. Restoring an existing DLL patch is still allowed.
public static class PlayerDebugging
{
    public static string DisabledReason => Loc.T("此版本已禁用播放器调试端口和 DLL 补丁；请使用系统媒体控制。旧补丁可在下方关闭，再退出并重新启动播放器。",
        "Player debugging ports and DLL patching are disabled in this build. Use system media controls. Turn off existing patches below, then quit and restart the player.");
    public static void DemandPermission() => throw new InvalidOperationException(DisabledReason);
}
