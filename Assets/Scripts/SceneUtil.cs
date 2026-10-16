using UnityEngine;
using UnityEngine.SceneManagement;

//这是静态工具类，专门用来统一处理场景加载
//封装场景跳转逻辑，加载场景前自动把游戏时间恢复正常（解除暂停）
public static class SceneUtil
{
    public static void Load(string sceneName)
    {
        Time.timeScale = 1f;
        Debug.Log($"[SceneUtil.Load] 跳转场景：{sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    public static void Load(int buildIndex)
    {
        //根据场景编号（BuildIndex）拿到场景完整资源路径，buildIndex=2 → 得到字符串"Assets/Scenes/Level2.unity"
        string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        //把上面的路径提取文件名，去掉后缀 .unity，输入Assets/Scenes/Level2.unity → 输出Level2
        string name = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        Load(name);
    }
}
