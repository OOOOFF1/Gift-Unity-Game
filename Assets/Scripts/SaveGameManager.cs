using UnityEngine;
using System.IO;
using System;
using static Cinemachine.DocumentationSortingAttribute;

//存档管理器（单例），负责把游戏数据存成 JSON 文件到本地硬盘
public class SaveGameManager : MonoBehaviour
{
    private static SaveGameManager _instance;
    public static SaveGameManager Instance
    {
        get
        {
            if (_instance != null)
                return _instance;
            // 场景中找
            _instance = FindObjectOfType<SaveGameManager>();
            if (_instance != null)
                return _instance;
            //直接双击关卡运行才会走到这里，打印显眼警告
            DebugUtil.LogWarning("未找到SaveGameManager，自动创建兜底实例！请务必从Start场景启动游戏！");
            GameObject obj = new GameObject("SaveGameManager_AutoCreate_DebugWarning");
            _instance = obj.AddComponent<SaveGameManager>();
            DontDestroyOnLoad(obj); //
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }

    // 每次构建 Demo 时，由构建脚本生成新的 ID
    private const string BuildIdFileName = "build_id.txt";

    // 记录本次 Demo 是否已经完成存档初始化
    private string GetBuildIdPath()
    {
        return Path.Combine(Application.persistentDataPath,BuildIdFileName);
    }

    private void InitializeSaveForNewBuild()
    {
        string buildIdPath = GetBuildIdPath();

        // 从 Demo 安装目录读取构建 ID
        string buildIdFile = Path.Combine(Application.streamingAssetsPath,BuildIdFileName);

        if (!File.Exists(buildIdFile))
        {
            DebugUtil.LogWarning("没有找到 build_id.txt，本次不自动清理存档。");
            return;
        }

        string currentBuildId = File.ReadAllText(buildIdFile).Trim();
        string savedBuildId = File.Exists(buildIdPath)? File.ReadAllText(buildIdPath).Trim(): "";

        if (string.IsNullOrEmpty(currentBuildId))
        {
            DebugUtil.LogWarning("构建 ID 为空，本次不自动清理存档。");
            return;
        }

        if (savedBuildId != currentBuildId)
        {
            // 只清理本游戏使用的关卡存档和解锁存档
            ClearAllLevelSaves();
            ClearUnlockSave();

            File.WriteAllText(buildIdPath, currentBuildId);

            DebugUtil.Log($"新 Demo 首次启动，已重置旧存档。Build ID={currentBuildId}");
        }
        else
        {
            DebugUtil.Log("当前 Demo 已初始化，保留现有游戏进度。");
        }
    }

    private void ClearAllLevelSaves()
    {
        // 你的 Build Settings 中关卡索引为 2、3、4
        for (int buildIndex = 2; buildIndex <= 4; buildIndex++)
        {
            DeleteLevelSave(buildIndex);
        }
    }
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DebugUtil.LogWarning("检测到重复 SaveGameManager，销毁重复对象");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSaveForNewBuild();

        DebugUtil.Log("SaveGameManager 初始化成功");
    }

    //如果这个是全局单例，把_instance置空，防止引用残留
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private string GetLevelSavePath(int buildIndex)
    {
        //返回某个关卡存档文件在硬盘上的完整路径
        //Application.persistentDataPath：Unity专门给游戏存存档的文件夹
        //Path.Combine(a,b)：拼接文件路径，自动处理斜杠/，不用自己手写\，跨平台兼容
        //$"level_{buildIndex}.json"：字符串插值，buildIndex = 2 → level_2.json
        return Path.Combine(Application.persistentDataPath, $"level_{buildIndex}.json");
    }

    public void SaveLevel(int buildIndex, LevelPlayerSaveData data)
    {
        if (data == null)
        {
            DebugUtil.LogWarning("SaveLevel：存档数据为空，无法保存！");
            return;
        }
        try
        {
            string json = JsonUtility.ToJson(data, true);
            string path = GetLevelSavePath(buildIndex);
            File.WriteAllText(path, json);
            DebugUtil.Log($"【保存成功】 BuildIndex={buildIndex} 路径={path}");
        }
        catch (System.Exception e)
        {
            DebugUtil.LogError($"保存关卡 BuildIndex={buildIndex} 失败：\n{e.Message}");
        }
    }

    public LevelPlayerSaveData LoadLevel(int buildIndex)
    {
        string path = GetLevelSavePath(buildIndex);
        if (!File.Exists(path))
        {
            DebugUtil.Log($"BuildIndex={buildIndex} 没有存档");
            return null;
        }
        try
        {
            string json = File.ReadAllText(path);
            LevelPlayerSaveData data = JsonUtility.FromJson<LevelPlayerSaveData>(json);
            if (data == null)
            {
                DebugUtil.LogWarning($" BuildIndex={buildIndex} 存档内容为空");
                return null;
            }
            if (data.eatenAppleIds == null)
            {
                data.eatenAppleIds = new System.Collections.Generic.List<int>();
            }
            if (data.killedEnemyIds == null)
            {
                data.killedEnemyIds = new System.Collections.Generic.List<int>();
            }
            DebugUtil.Log($"📖 读取 BuildIndex={buildIndex} 存档成功");
            return data;
        }
        catch (System.Exception e)
        {
            DebugUtil.LogError($"❌ 读取关卡 BuildIndex={buildIndex} 存档失败：\n{e.Message}");
            return null;
        }
    }

    public void DeleteLevelSave(int buildIndex)
    {
        string path = GetLevelSavePath(buildIndex);
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                DebugUtil.Log($"🗑 删除关卡 BuildIndex={buildIndex} 本地存档");
            }
            else
            {
                DebugUtil.Log($"ℹ️ BuildIndex={buildIndex} 没有需要删除的存档");
            }
        }
        catch (System.Exception e)
        {
            DebugUtil.LogError($"❌ 删除关卡 BuildIndex={buildIndex} 存档失败：\n{e.Message}");
        }
    }

    private string GetUnlockSavePath()
    {
        return Path.Combine(Application.persistentDataPath, "unlockData.json");
    }

    public int GetUnlockedLevel()
    {
        string path = GetUnlockSavePath();
        if (!File.Exists(path))
        {
            return 1;
        }
        try
        {
            string json = File.ReadAllText(path);
            UnlockSaveData data = JsonUtility.FromJson<UnlockSaveData>(json);
            if (data != null)
            {
                //原来 return Mathf.Clamp(data.unlockedMaxLevel, 1, 3);
                return Mathf.Max(1, data.unlockedMaxLevel);

            }
        }
        catch (System.Exception e)
        {
            DebugUtil.LogError($"❌ 读取关卡解锁存档失败：\n{e.Message}");
        }
        return 1;
    }

    public bool IsLevelCompleted(int levelNumber)
    {
        int unlocked = GetUnlockedLevel();
        return levelNumber >= 1 && levelNumber < unlocked;
    }


    public void UnlockLevel(int levelNumber)
    {
        if (levelNumber < 1)
        {
            DebugUtil.LogWarning($"⚠️ UnlockLevel 收到非法关卡编号：{levelNumber}");
            return;
        }
        int currentMax = GetUnlockedLevel();
        if (levelNumber <= currentMax)
        {
            DebugUtil.Log($"🔐 Level{levelNumber} 已经解锁，当前最高解锁 Level{currentMax}");
            return;
        }
        UnlockSaveData data = new UnlockSaveData();
        data.unlockedMaxLevel = levelNumber;
        try
        {
            string json = JsonUtility.ToJson(data, true);
            string path = GetUnlockSavePath();
            File.WriteAllText(path, json);
            DebugUtil.Log($"🔓【解锁成功】Level{levelNumber}");
            DebugUtil.Log($"🔐 当前最高解锁关卡：Level{levelNumber}");
        }
        catch (System.Exception e)
        {
            DebugUtil.LogError($"❌ 写入关卡解锁存档失败：\n{e.Message}");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("清除关卡解锁存档(测试)")]
#endif
    public void ClearUnlockSave()
    {
        string path = GetUnlockSavePath();
        if (File.Exists(path))
        {
            File.Delete(path);
            DebugUtil.Log("✅ 已清除关卡解锁存档，重置为仅解锁 Level1");
        }
        else
        {
            DebugUtil.Log("ℹ️ 没有找到解锁存档，当前默认仅解锁 Level1");
        }
    }

}

[System.Serializable]
public class UnlockSaveData
{
    public int unlockedMaxLevel;
}
