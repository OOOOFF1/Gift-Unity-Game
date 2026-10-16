using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour
{
    [Header("关卡按钮预制体")]
    public GameObject levelBtnPrefab;
    [Header("按钮容器：Hierarchy里的Grid")]
    ////UGUI的Grid布局物体，生成的按钮全部放在它下面自动排版
    public Transform contentRoot;
    [Header("翻页按钮")]
    public Button prevPageBtn;
    public Button nextPageBtn;
    [Header("配置")]
    public int levelPerPageRow = 3;
    public int columnCount = 5;

    private int currentPage = 0;
    private int unlockedLevel;

    //保存当前页面生成出来的所有按钮，翻页的时候把这些旧按钮全部销毁
    //readonly：只读修饰符，含义：这个变量本身不能再重新赋值新对象，只能在声明处 / 构造函数赋值一次
    private readonly List<GameObject> spawnedBtns = new List<GameObject>();

    private void Start()
    {
        //给翻页按钮绑定点击函数；先Remove再Add，防止重复绑定，点一次触发多次
        if (prevPageBtn != null)
        {
            prevPageBtn.onClick.RemoveListener(PagePrev);
            prevPageBtn.onClick.AddListener(PagePrev);
        }
        if (nextPageBtn != null)
        {
            nextPageBtn.onClick.RemoveListener(PageNext);
            nextPageBtn.onClick.AddListener(PageNext);
        }

        // 新增：订阅场景加载事件，让 OnAnySceneLoaded 生效
        // 只要任何场景加载完成，就执行OnAnySceneLoaded      
        SceneManager.sceneLoaded -= OnAnySceneLoaded;
        SceneManager.sceneLoaded += OnAnySceneLoaded;

        RefreshUnlockAndUI();//读取存档，刷新关卡按钮UI
    }

    // 场景加载完成后，如果进入Select则刷新关卡UI，SceneManager.sceneLoaded事件的回调函数
    private void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //只有加载进来的场景是选关场景Select，才刷新UI
        if (scene.name == GameConst.Scene_Select)
        {
            RefreshUnlockAndUI();
        }
    }

    private void OnDestroy()
    {
        //取消订阅事件，防止幽灵回调（物体销毁了还被系统调用函数）
        SceneManager.sceneLoaded -= OnAnySceneLoaded;
    }

    //自动扫描Build设置，统计所有Level开头的关卡场景数量
    private int GetTotalLevelCount()
    {
        int count = 0;
        //遍历Build Settings里面所有添加的场景
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            //根据场景的 Build 索引i，获取这个场景硬盘完整文件路径
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            //GetFileNameWithoutExtension(path)：把上面完整路径处理，去掉文件夹、去掉后缀.unity，只拿到场景名字，得到Level1
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            //名字以 Level 大写开头，算作一个关卡
            if (sceneName.StartsWith("Level"))
            {
                count++;
            }
        }
        return count;
    }

    //读取解锁存档，准备刷新页面
    private void RefreshUnlockAndUI()
    {
        //读取存档管理器，拿到玩家最高解锁关卡
        if (SaveGameManager.Instance != null)
        {
            unlockedLevel = SaveGameManager.Instance.GetUnlockedLevel();
        }
        else
        {
            unlockedLevel = 1;
            Debug.LogWarning( " SaveGameManager.Instance 不存在，默认只解锁 Level1");
        }
        Debug.Log($"当前最高解锁关卡：Level{unlockedLevel}");
        RefreshPage();//真正生成按钮
    }

    //刷新当前页面
    private void RefreshPage()
    {
        int totalAllLevel = GetTotalLevelCount();//自动拿到一共有多少关卡
        int levelPerPage = levelPerPageRow * columnCount;
        //向下取整
        int maxPageIndex = totalAllLevel > 0 ? (totalAllLevel - 1) / levelPerPage : 0;

        //把上一页所有生成出来的按钮全部销毁，清空列表
        //如果翻页不销毁旧按钮：新一页按钮会叠加在旧按钮上面，屏幕一堆重叠按钮，UI 乱掉
        foreach (GameObject btn in spawnedBtns)
        {
            if (btn != null)
            {
                Destroy(btn);
            }
        }
        spawnedBtns.Clear();
       
        int startIndex = currentPage * levelPerPage;//当前页从第几关开始

        List<GameObject> newPageButtons = new List<GameObject>();
        //实例化按钮预制体，父物体设置为contentRoot（Grid自动排版）
        for (int i = 0; i < levelPerPage; i++)
        {
            int levelNum = startIndex + i + 1;
            GameObject btnObj = Instantiate(levelBtnPrefab,contentRoot);
            
            btnObj.SetActive(false);//先生成立刻隐藏，全部配置完再显示，消除UI闪烁
            spawnedBtns.Add(btnObj);
            newPageButtons.Add(btnObj);

            Button btnComp = btnObj.GetComponent<Button>();
            if (btnComp == null)
            {
                Debug.LogError( $"{levelBtnPrefab.name} 上没有 Button 组件！");
                continue;
            }
            
            btnComp.onClick.RemoveAllListeners();//清空旧点击事件           
            btnComp.interactable = false;//默认按钮不可点击

            //找按钮子物体 BtnText 文字组件
            Transform textTrans = btnObj.transform.Find("BtnText");
            Text txtComp = null;
            if (textTrans != null)
            {
                txtComp = textTrans.GetComponent<Text>();
            }

            if (levelNum > totalAllLevel)
            {
                if (txtComp != null)
                {
                    txtComp.text = "";
                }
                continue;
            }

            // 设置关卡数字
            if (txtComp != null)
            {
                txtComp.text = levelNum.ToString();
            }

            string sceneName = $"Level{levelNum}";
            bool sceneExists = IsSceneInBuild(sceneName);//检查这个关卡场景是否加到Build列表
            //levelNum：当前这个按钮代表第几关（比如 4）,unlockedLevel：从存档读取，玩家最高解锁到第几关
            bool isUnlock = levelNum <= unlockedLevel;//判断关卡是否已经解锁

            // 最终是否允许点击
            bool canClick = levelNum <= totalAllLevel && isUnlock && sceneExists;
            btnComp.interactable = canClick;

            //ColorBlock是 Unity UI Button 的颜色配置结构体
            ColorBlock colors = btnComp.colors;
            if (!canClick)
            {
                colors.disabledColor = Color.gray;//禁用状态颜色
            }
            btnComp.colors = colors;

            //可以点击：绑定点击事件，点击调用LoadLevelScene加载关卡
            if (canClick)
            {
                int targetLevel = levelNum;
                btnComp.onClick.AddListener(() =>
                {
                    LoadLevelScene(targetLevel);
                });
            }
        }

        //所有按钮全部配置完毕，再一次性SetActive(true)显示全部按钮，避免闪烁
        foreach (GameObject btnObj in newPageButtons)
        {
            if (btnObj != null)
            {
                btnObj.SetActive(true);
            }
        }

        //控制翻页按钮是否可以点击
        if (prevPageBtn != null)
        {
            //控制翻页按钮是否可以点击
            prevPageBtn.interactable = currentPage > 0;
        }
        if (nextPageBtn != null)
        {
            //没有下一页就灰色
            nextPageBtn.interactable = currentPage < maxPageIndex;
        }
    }

    /// <summary>
    /// 判断指定场景是否存在于Build Settings
    /// </summary>
    private bool IsSceneInBuild(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings;i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    private void LoadLevelScene(int levelNum)
    {
        int totalAllLevel = GetTotalLevelCount();
        // 检查关卡编号
        if (levelNum < 1 || levelNum > totalAllLevel)
        {
            Debug.LogWarning($"无效关卡编号：{levelNum}");
            return;
        }

        // 再次检查是否解锁
        if (levelNum > unlockedLevel)
        {
            Debug.LogWarning($"Level{levelNum} 尚未解锁！" +$"当前最高解锁：Level{unlockedLevel}");
            return;
        }

        // 获取场景名字
        string sceneName = $"Level{levelNum}";
        // 检查场景是否存在于Build Settings
        if (!IsSceneInBuild(sceneName))
        {
            Debug.LogError($"❌ 场景 {sceneName} 不存在于 Build Settings！");
            return;
        }
        
        Debug.Log($"进入 {sceneName}");
        SceneUtil.Load(sceneName);//加载关卡场景
    }

    // 上一页
    private void PagePrev()
    {
        if (currentPage <= 0)
        {
            return;
        }
        currentPage--;
        RefreshPage();//销毁旧按钮，生成新一页按钮
    }

    //下一页
    private void PageNext()
    {
        int totalAllLevel = GetTotalLevelCount();
        int levelPerPage = levelPerPageRow * columnCount;
        int maxPageIndex = totalAllLevel > 0 ? (totalAllLevel - 1) / levelPerPage : 0;
        if (currentPage >= maxPageIndex)
        {
            return;
        }
        currentPage++;
        RefreshPage();
    }
}
