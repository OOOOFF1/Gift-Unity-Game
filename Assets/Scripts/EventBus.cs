using System;//Action<T>委托（就是可以存函数的类型，广播收到消息后要跑的函数）
using System.Collections.Generic;//Dictionary、List容器
using UnityEngine;

/// <summary>
/// 极简事件总线，降低模块耦合；订阅后务必在OnDestroy取消订阅，防止内存泄漏、空回调
/// 使用List存储回调，彻底解决原生Delegate.Remove移除不稳定的坑
/// </summary>
//静态类，不用挂任何物体，直接EventBus.函数()调用
public static class EventBus
{
    //_eventTable = 广播台的登记簿（Dictionary 字典）
    //Key（左边）：频道名字，字符串，比如EVENT_REACH_FINISH（到达终点频道）
    //Value（右边）：这个频道下面，所有听众函数的清单（List 列表）
    //登记簿全程唯一，游戏运行期间一直存在，readonly：变量一旦赋值之后，不能再把_eventTable指向别的字典
    //只能修改字典里面的内容（添加频道、删听众），但是不能写_eventTable = new Dictionary...重新换一本全新登记簿
    private static readonly Dictionary<string, object> _eventTable = new Dictionary<string, object>();

    //Action<T>：可以保存一个函数，T 代表这个函数接收的“快递数据包”类型。这个函数有 1 个参数，没有返回值
    //去登记簿查找某个频道的听众名单
    private static List<Action<T>> GetList<T>(string eventName, bool createIfMissing = true)
    {
        //字典.TryGetValue(要查找的key, out 拿到的值存到这个变量)
        if (_eventTable.TryGetValue(eventName, out var rawList))
        {
            //字典_eventTable定义是Dictionary<string, object>，存进去的 List 被包装成 object 类型
            //as：安全强制类型转换
            return rawList as List<Action<T>>;
        }
        //createIfMissing是一个开关：找不到频道的时候，要不要自动新建频道 + 空白听众清单
        //=true是默认值：调用 GetList 的时候，如果不写第二个参数，自动当成createIfMissing = true
        //什么时候传 true？（Subscribe 订阅的时候），什么时候传 false？（Publish 广播 / UnSubscribe 取消订阅）
        if (createIfMissing)
        {
            List<Action<T>> newList = new List<Action<T>>();
            _eventTable.Add(eventName, newList);
            return newList;
        }
        return null;
    }

    /// <summary>订阅事件，自动去重，防止重复订阅执行多次</summary>
    // eventName：频道名称，callback：你准备用来接收广播的函数名字
    public static void Subscribe<T>(string eventName, Action<T> callback)
    {
        if (callback == null) return;
        //调用 GetList 拿到该频道听众清单（找不到频道就自动新建）
        List<Action<T>> list = GetList<T>(eventName);
        if (!list.Contains(callback))//!list.Contains(callback) 防止重复订阅
        {
            list.Add(callback);
        }
    }

    /// <summary>取消订阅</summary>
    //写了 Subscribe，就一定要在 OnDestroy 写 UnSubscribe
    public static void UnSubscribe<T>(string eventName, Action<T> callback)
    {
        if (callback == null) return;
        List<Action<T>> list = GetList<T>(eventName, false);
        if (list != null)
        {
            list.Remove(callback);
        }
    }

    /// <summary>派发事件</summary>
    //arg：快递数据包（T 类型，事件类，比如 ReachFinishEvent）
    public static void Publish<T>(string eventName, T arg)
    {
        List<Action<T>> list = GetList<T>(eventName, false);
        if (list == null) return;
        // ToArray拷贝快照，防止回调内部订阅/取消订阅导致遍历集合修改异常
        //如果直接循环原始 list，在执行监听函数的时候，脚本内部又订阅 / 取消订阅，会修改 list（增删元素）
        //C# 有一条硬性规则：遍历 List 的同时修改 List，直接抛异常：集合已修改；可能无法执行枚举操作，游戏报错崩溃
        //ToArray()把当前这一刻的听众列表复制一份全新数组（快照），循环遍历这个复制出来的数组
        //就算原始 List 在广播过程中增删、修改，不影响已经复制好的快照数组，不会报错
        Action<T>[] snapshot = list.ToArray();
        foreach (var cb in snapshot)
        {
            //cb?.Invoke(arg)：执行这个函数，把快递数据包 arg 传给它，?.代表 cb 不为空才执行，防止空报错
            cb?.Invoke(arg);
        }
    }

    // ========== 事件常量定义，全部写在这里，避免字符串硬编码 ==========
    public const string EVENT_PLAYER_DEATH = "EVENT_PLAYER_DEATH";
    public const string EVENT_PLAYER_WIN = "EVENT_PLAYER_WIN";
    public const string EVENT_ENEMY_DEAD = "EVENT_ENEMY_DEAD";
    public const string EVENT_APPLE_PICKUP = "EVENT_APPLE_PICKUP";
    public const string EVENT_REACH_FINISH = "EVENT_REACH_FINISH";
    public const string EVENT_DIALOGUE_FINISHED = "EVENT_DIALOGUE_FINISHED";
}

/// <summary>玩家死亡事件参数</summary>
[Serializable]
public class PlayerDeathEvent
{
    public int currentLevelBuildIndex;
}
/// <summary>玩家通关事件参数</summary>
[Serializable]
public class PlayerWinEvent
{
    public int currentLevelBuildIndex;
    public int nextBuildIndex;
}
/// <summary>敌人死亡事件参数</summary>
[Serializable]
public class EnemyDeadEvent
{
    public int enemyId;
}
/// <summary>苹果拾取事件参数</summary>
[Serializable]
public class ApplePickupEvent
{
    public int appleId;
}
/// <summary>玩家触碰终点事件</summary>
[Serializable]
public class ReachFinishEvent
{
}
/// <summary>对话结束事件参数</summary>
[Serializable]
public class DialogueFinishedEvent
{
}

