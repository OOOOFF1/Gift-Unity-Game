using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStatus : MonoBehaviour
{
    [Header("死亡动画参数")]
    public float deathAnimTime = 0.8f;
    public float waitAfterAnim = 0.6f;

    [Header("附属物体")]
    public GameObject playerPS;//玩家的粒子特效物体
    public GameObject weapons;

    private Rigidbody2D rb;
    private Animator anim;
    private PlayerMovement movement;
    private PlayerHealth playerHealth;

    //把动画参数名字字符串`"death"`转换成一个数字哈希值
    private static readonly int DeathHash =Animator.StringToHash("death");

    public bool isDead;
    public bool isWin;

    // 0 = Start
    // 1 = Select
    // 2 = Level1
    private const int firstLevelBuildIndex = 2;

    void Awake()
    {
        isDead = false;
        isWin = false;

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void OnDestroy()
    {
        StopAllCoroutines();//玩家物体销毁，停止所有协程，防止幽灵协程
    }

    public void Death()
    {
        //保护：如果已经死了，直接 return，防止重复执行死亡逻辑，避免重复发死亡事件、重复播放死亡动画
        if (isDead)
            return;
        isDead = true;
        //调用 PlayerHealth 的ForceStopFlash()，强制停止受伤红色闪烁，把玩家贴图颜色恢复正常
        playerHealth?.ForceStopFlash();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;//刚体速度清零，玩家不再移动
            rb.angularVelocity = 0f;//角速度清零，不再旋转
            rb.bodyType = RigidbodyType2D.Static;//刚体设置为Static静态，死亡之后不受重力、碰撞影响，玩家钉住不动
        }
        if (movement != null)
            movement.enabled = false;//把PlayerMovement移动脚本关闭
        if (playerPS != null)
            Destroy(playerPS);
        if (weapons != null)
            Destroy(weapons);
        anim?.SetTrigger(DeathHash);//给动画机的death触发器赋值，播放玩家死亡动画

        //拿到当前正在运行关卡场景的BuildIndex 编号
        int curBuildIndex = SceneManager.GetActiveScene().buildIndex;
        PlayerDeathEvent deathEvt = new PlayerDeathEvent
        {
            currentLevelBuildIndex = curBuildIndex
        };
        EventBus.Publish(EventBus.EVENT_PLAYER_DEATH, deathEvt);
    }

    public void WinGame()
    {
        //如果玩家已经死亡 → 不能通关
        //如果已经通关过一次 → 不能重复执行通关逻辑，直接 return。防止多次触发终点，重复发通关事件
        if (isDead || isWin)
        {
            DebugUtil.LogWarning("⚠️ WinGame 被拦截！");
            return;
        }
        isWin = true;
        playerHealth?.ForceStopFlash();
        int curBuildIndex = SceneManager.GetActiveScene().buildIndex;
        int nextBuildIndex = curBuildIndex + 1;

        if (movement != null)
            movement.enabled = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }
        Time.timeScale = 0f;

        PlayerWinEvent winEvt = new PlayerWinEvent
        {
            currentLevelBuildIndex = curBuildIndex,
            nextBuildIndex = nextBuildIndex
        };
        EventBus.Publish(EventBus.EVENT_PLAYER_WIN, winEvt);
    }

    public void BackToLevelSelectAfterWin()
    {
        Time.timeScale = 1;
        SceneUtil.Load(GameConst.Scene_Select);
    }

    public void NextLevel()
    {
        Time.timeScale = 1;
        int curBuildIndex =SceneManager.GetActiveScene().buildIndex;
        int nextBuildIndex =curBuildIndex + 1;
        //sceneCountInBuildSettings：返回 Build Settings 窗口里面添加的场景总数量
        if (nextBuildIndex >=SceneManager.sceneCountInBuildSettings)
        {
            SceneUtil.Load(GameConst.Scene_Select);
            return;
        }

        SceneUtil.Load(nextBuildIndex);
    }

#if UNITY_EDITOR

    [ContextMenu("调试：模拟触发WinGame通关")]
    private void DebugTriggerWinGame()
    {
        WinGame();
    }

    [ContextMenu("调试：模拟死亡Death")]
    private void DebugTriggerDeath()
    {
        Death();
    }

#endif
}