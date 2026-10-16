using UnityEngine;

//静态工具类, static 静态类：不用挂物体，直接 类名.函数() 就能调用
//在 Unity 编辑器里面点 Play 测试游戏：存在UNITY_EDITOR符号
//打包 exe（玩家运行游戏）：这个符号直接消失，不存在
public static class DebugUtil
{
    /// <summary>普通调试日志，仅编辑器生效，打包直接移除</summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(string msg)
    {
        Debug.Log(msg);
    }

    /// <summary>警告日志，编辑器+打包均保留</summary>
    public static void LogWarning(string msg)
    {
        Debug.LogWarning($"[WARN] {msg}");
    }

    /// <summary>错误日志，编辑器+打包均保留</summary>
    public static void LogError(string msg)
    {
        Debug.LogError($"[ERR] {msg}");
    }
}
