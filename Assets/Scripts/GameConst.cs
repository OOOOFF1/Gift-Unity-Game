/// <summary>
/// 全局常量：Tag、场景名，消除代码硬编码字符串
/// 后续改Tag/场景名，只改这一处，不用全工程搜索替换
/// </summary>
public static class GameConst
{
    // Tag
    public const string Tag_Player = "Player";
    public const string Tag_Enemy = "Enemy";
    public const string Tag_Trap = "Trap";
    public const string Tag_DieLine = "DieLine";
    public const string Tag_OneWayPlatform = "OneWayPlatform";
    public const string Tag_Teleporter = "Teleporter";

    // 场景名称
    public const string Scene_Select = "Select";
}
