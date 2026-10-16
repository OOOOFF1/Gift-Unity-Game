using UnityEngine;

/// <summary>
/// 泛型DontDestroyOnLoad单例基类，仅给全局DDOL管理器使用
/// 注意：现有AudioManager/SaveGameManager/SceneTransition不修改继承，仅后续新增管理器使用
/// </summary>
//public class Singleton<T>：泛型类，T 代表你将来要做成单例的那个管理器类型
//where T : MonoBehaviour泛型约束：限制 T 必须是 MonoBehaviour 或者它的子类（必须能挂载到 GameObject 上）
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            return _instance;
        }
    }

    //protected：只有本类和子类可以访问
    //virtual：虚函数，子类可以重写 Awake
    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
