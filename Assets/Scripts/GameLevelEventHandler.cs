using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

//GameLevelEventHandler= 关卡事件总管家
//专门监听 EventBus 广播的各种游戏事件（玩家死、通关、敌人死亡、捡苹果、碰到终点），收到广播后执行对应的逻辑（弹出弹窗、保存数据、加载场景、记录苹果 / 击杀敌人）
public class GameLevelEventHandler : MonoBehaviour
{
    [Header("死亡弹窗")]
    public PopupTypeWriter deathPopupWriter;
    [Header("通关弹窗")]
    public PopupTypeWriter winPopupWriter;
    [Header("全部通关弹窗")]
    public PopupTypeWriter allClearPopup;

    //缓存事件数据包，协程是延迟执行的，广播传过来的evt参数，协程晚一点才会用到，所以先存起来
    private PlayerDeathEvent _cachedDeathEvt;
    private PlayerWinEvent _cachedWinEvt;

    private void Awake()
    {
        EventBus.Subscribe<PlayerDeathEvent>(EventBus.EVENT_PLAYER_DEATH, OnPlayerDeathEvent);
        EventBus.Subscribe<PlayerWinEvent>(EventBus.EVENT_PLAYER_WIN, OnPlayerWinEvent);
        EventBus.Subscribe<EnemyDeadEvent>(EventBus.EVENT_ENEMY_DEAD, OnEnemyDead);
        EventBus.Subscribe<ApplePickupEvent>(EventBus.EVENT_APPLE_PICKUP, OnApplePickup);
        EventBus.Subscribe<ReachFinishEvent>(EventBus.EVENT_REACH_FINISH, OnReachFinish);
    }

    private void OnDestroy()
    {
        EventBus.UnSubscribe<PlayerDeathEvent>(EventBus.EVENT_PLAYER_DEATH, OnPlayerDeathEvent);
        EventBus.UnSubscribe<PlayerWinEvent>(EventBus.EVENT_PLAYER_WIN, OnPlayerWinEvent);
        EventBus.UnSubscribe<EnemyDeadEvent>(EventBus.EVENT_ENEMY_DEAD, OnEnemyDead);
        EventBus.UnSubscribe<ApplePickupEvent>(EventBus.EVENT_APPLE_PICKUP, OnApplePickup);
        EventBus.UnSubscribe<ReachFinishEvent>(EventBus.EVENT_REACH_FINISH, OnReachFinish);
        StopAllCoroutines();
    }

    //收到【玩家死亡】广播，启动协程
    private void OnPlayerDeathEvent(PlayerDeathEvent evt)
    {
        StartCoroutine(Co_ShowDeathPopup(evt));
    }

    private IEnumerator Co_ShowDeathPopup(PlayerDeathEvent evt)
    {
        yield return new WaitForSeconds(0.8f);
        yield return new WaitForSeconds(0.6f);
        if (this == null) yield break;//如果物体已经销毁，直接终止协程，不再执行
        _cachedDeathEvt = evt;//把死亡事件数据包存到_cachedDeathEvt
        if (deathPopupWriter != null)
        {
            deathPopupWriter.OnBtnFirstClick = OnDeathRestart;
            deathPopupWriter.OnBtnSecondClick = OnDeathBackSelect;
            deathPopupWriter.ShowPopup();//弹窗出现，文字打字机效果
        }
    }

    private void OnDeathRestart()
    {
        if (_cachedDeathEvt == null) return;
        if (SaveGameManager.Instance != null)
            SaveGameManager.Instance.DeleteLevelSave(_cachedDeathEvt.currentLevelBuildIndex);
        //恢复游戏速度（死亡的时候你大概率会把游戏暂停 timeScale=0）
        Time.timeScale = 1;
        SceneUtil.Load(_cachedDeathEvt.currentLevelBuildIndex);
    }

    private void OnDeathBackSelect()
    {
        if (_cachedDeathEvt == null) return;
        if (SaveGameManager.Instance != null)
            SaveGameManager.Instance.DeleteLevelSave(_cachedDeathEvt.currentLevelBuildIndex);
        Time.timeScale = 1;
        SceneUtil.Load(GameConst.Scene_Select);
    }

    //收到「玩家通关」广播，启动协程，执行展示通关弹窗的逻辑
    private void OnPlayerWinEvent(PlayerWinEvent evt)
    {
        StartCoroutine(Co_ShowWinPopup(evt));
    }

    private IEnumerator Co_ShowWinPopup(PlayerWinEvent evt)
    {
        yield return null;
        //安全保护：如果这个脚本已经被销毁，直接停止协程，不再跑后面代码，防止报错
        if (this == null) yield break;
        //把通关数据包缓存起来，后面弹窗点击的时候要用
        _cachedWinEvt = evt;
        //拿到 Build 面板里一共有多少个场景
        int totalSceneCount = SceneManager.sceneCountInBuildSettings;
        //你的场景索引是从 0 开始，关卡显示序号从 1 开始，所以减 1 做转换
        int currentLevelNumber = evt.currentLevelBuildIndex - 1;

        if (evt.nextBuildIndex < totalSceneCount)
        {
            int nextLevelNumber = currentLevelNumber + 1;
            if (SaveGameManager.Instance != null)
            {
                SaveGameManager.Instance.UnlockLevel(nextLevelNumber);
            }
        }

        if (SaveGameManager.Instance != null)
        {
            SaveGameManager.Instance.DeleteLevelSave(evt.currentLevelBuildIndex);
        }

        bool isRealLastLevel = evt.currentLevelBuildIndex >= 2 && evt.nextBuildIndex >= totalSceneCount;
        if (isRealLastLevel)
        {
            if (allClearPopup != null)
            {
                allClearPopup.OnBtnFirstClick = OnAllClearBackSelect;
                allClearPopup.OnBtnSecondClick = null;
                allClearPopup.ShowPopup();
            }
        }
        else
        {
            if (winPopupWriter != null)
            {
                winPopupWriter.OnBtnFirstClick = OnWinGoNextLevel;
                winPopupWriter.OnBtnSecondClick = OnWinBackSelect;
                winPopupWriter.ShowPopup();
            }
        }
    }

    private void OnWinGoNextLevel()
    {
        if (_cachedWinEvt == null) return;
        Time.timeScale = 1;
        SceneUtil.Load(_cachedWinEvt.nextBuildIndex);
    }

    private void OnWinBackSelect()
    {
        Time.timeScale = 1;
        SceneUtil.Load(GameConst.Scene_Select);
    }

    private void OnAllClearBackSelect()
    {
        Time.timeScale = 1;
        SceneUtil.Load(GameConst.Scene_Select);
    }

    /// <summary>订阅敌人死亡事件，记录击杀ID【每次使用动态Find，不缓存】</summary>
    private void OnEnemyDead(EnemyDeadEvent evt)
    {
        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            player.AddKilledEnemyId(evt.enemyId);
        }
    }

    /// <summary>订阅拾取苹果事件，记录苹果【每次使用动态Find，不缓存】</summary>
    private void OnApplePickup(ApplePickupEvent evt)
    {
        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            player.appleCount++;
            player.AddEatenAppleId(evt.appleId);
            if (player.appleText != null)
            {
                player.appleText.text = $"Apples:{player.appleCount}";
            }
        }
    }

    private void OnReachFinish(ReachFinishEvent evt)
    {
        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            PlayerStatus ps = player.GetComponent<PlayerStatus>();
            //如果 ps 不是空，才执行后面的 WinGame ()；如果 ps 是 null，直接什么都不做，不会报空引用错误
            ps?.WinGame();
        }
    }
}
