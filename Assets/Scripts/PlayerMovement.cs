using System.Collections;//协程
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    Rigidbody2D rb;
    Animator ani;
    //[Range(1,10)]：Inspector 面板变成滑动条，数值只能 1~10。跳跃向上冲量大小
    [Range(1, 10)] public float jumpSpeed = 5f;
    [SerializeField] float playerSpeed = 5f;//水平移动速度
    //下落重力倍数。下落时重力放大，手感更利落，不会轻飘飘
    [SerializeField] float fallMultiplier = 2.5f;
    //松开跳跃键时，上升阶段的重力倍数。松开空格，快速结束跳高，平台跳跃经典手感
    [SerializeField] float upMultiplier = 1.5f;
    //剩余跳跃次数，默认2次→二段跳。踩地重置回 2
    [SerializeField] int jumpCount = 2;

    float x;
    bool jump;
    bool jumpHold;
    bool isJumping;//标记本次跳跃触发

    private bool canDash = true;//是否可以冲刺（冲刺冷却锁）
    private bool isDashing;//标记正在冲刺中，冲刺期间关闭移动、跳跃、踩踏敌人逻辑
    [SerializeField] private float dashingPower = 20f;//冲刺力度
    [SerializeField] private float dashingTime = 0.2f;//冲刺持续时间 0.2 秒
    [SerializeField] private float noDashTime = 1f;//冲刺冷却时间 1 秒，冲完等 1 秒才能再次冲刺

    private int facingDirection = 1;//玩家朝向：1 = 朝右，-1 = 朝左，控制角色翻转

    [SerializeField] bool isGrounded;//是否踩在地面上
    [SerializeField] Transform groundCheck;//脚底空物体，用来做地面检测
    [SerializeField] LayerMask ground;//地面 Layer 层，只检测这一层判断落地
    [SerializeField] ParticleSystem playerPS;//跳跃粒子特效，起跳时播放
    [SerializeField] Transform killPoint;//用来踩踏敌人判定

    public GameObject blood;//击杀敌人时生成的血迹预制体

    [Header("收集苹果")]
    public int appleCount;
    public Text appleText;

    //m_eatenAppleIds：已经拾取的苹果 ID 列表（存档核心数据）
    //m_killedEnemyIds：已经击杀敌人 ID 列表
    private List<int> m_eatenAppleIds = new List<int>();
    private List<int> m_killedEnemyIds = new List<int>();

    //只读属性（只读副本），=>是 C# 表达式体属性
    //new List<int>(m_eatenAppleIds)：m_是项目命名规范：代表私有成员变量。新建一个全新的 List，把 m_eatenAppleIds 里面所有数据复制进去
    public List<int> EatenAppleIds => new List<int>(m_eatenAppleIds);
    public List<int> KilledEnemyIds => new List<int>(m_killedEnemyIds);

    [Header("头顶踩敌人检测框尺寸")]
    public Vector2 killPointBoxSize = new Vector2(0.2f, 0.4f);

    private PlayerHealth playerHealth;
    private PlayerStatus playerStatus;

    //脚步声定时器，防止每物理帧播放叠音
    private float _footstepTimer = 0f;//脚步声计时器
    private const float FootstepInterval = 0.3f;//脚步声最小间隔 0.3 秒

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ani = GetComponent<Animator>();

        playerHealth = GetComponent<PlayerHealth>();
        playerStatus = GetComponent<PlayerStatus>();
    }

    void Start()
    {
        killPoint = transform.Find("KillPoint");
        StartCoroutine(LoadSaveDelayed());//启动协程 LoadSaveDelayed ()，读取关卡存档
    }

    private IEnumerator LoadSaveDelayed()
    {
        int curLevel = SceneManager.GetActiveScene().buildIndex;//当前场景 BuildIndex
        int currentLevelNum = curLevel - 1;//关卡编号
        LevelPlayerSaveData save = null;
        SaveGameManager saveManager = SaveGameManager.Instance;

        // 有存档
        if (saveManager != null)
        {
            save = saveManager.LoadLevel(curLevel);
        }

        if (save != null)
        {
            //立即恢复玩家位置
            transform.position = new Vector2(save.playerX,save.playerY);

            //恢复生命值
            if (playerHealth != null)
            {
                playerHealth.SetHealthForLoadSave(save.currentHP);
            }

            //恢复苹果数量
            appleCount = save.appleCount;
            if (appleText != null)
            {
                appleText.text =$"Apples:{appleCount}";
            }

            //防止列表为空。如果是 null，就新建一个空 List。保证后面遍历的时候，一定是一个合法的列表（哪怕里面是空），不会崩溃
            if (save.eatenAppleIds == null)
            {
                save.eatenAppleIds = new List<int>();
            }
            if (save.killedEnemyIds == null)
            {
                save.killedEnemyIds = new List<int>();
            }
            //删除已经吃掉的苹果
            DestroyEatenApplesFromSave(save);
            //删除已经击杀的敌人
            DestroyKilledEnemiesFromSave(save);

            //恢复ID列表，把存档里的 ID 列表拷贝到玩家内部私有列表
            m_eatenAppleIds = new List<int>(save.eatenAppleIds);
            m_killedEnemyIds = new List<int>(save.killedEnemyIds);
            DebugUtil.Log($"读取 Level{currentLevelNum} 存档，" +$"直接进入存档位置 X={save.playerX:F2} Y={save.playerY:F2}");
        }

        // 没有存档，苹果清零，清空 ID 列表，UI 重置
        else
        {
            appleCount = 0;
            m_eatenAppleIds.Clear();
            m_killedEnemyIds.Clear();

            if (appleText != null)
            {
                appleText.text =$"Apples:{appleCount}";
            }

            DebugUtil.Log($"Level{currentLevelNum} 没有存档，从头开始");
        }
        yield break;//直接终止这个协程
    }

    void Update()
    {
        if (playerStatus != null &&(playerStatus.isDead ||playerStatus.isWin))
        {
            return;//玩家死亡 / 通关胜利 → Update 直接 return，所有输入失效
        }

        if (Mathf.Abs(Time.timeScale) < 0.001f) return;

        if (isDashing)
        {
            return;//正在冲刺，跳过所有按键逻辑
        }

        x = Input.GetAxis("Horizontal");//水平轴输入，A/D 左右轴
        jump = Input.GetButtonDown("Jump");//按下空格那一帧触发一次
        jumpHold = Input.GetButton("Jump");//长按空格持续 true

        // 跳跃，按下空格，并且还有剩余跳跃次数
        if (jump && jumpCount > 0)
        {
            isJumping = true;
            jumpCount--;
            PPS();
            AudioManager.Instance?.PlaySound(AudioManager.Instance.jumpSound);
        }

        // 冲刺，Input.GetKey：按住持续检测，不是按下瞬间
        if (Input.GetKey(KeyCode.LeftShift) &&canDash)//按下左 Shift，并且冷却结束
        {
            StartCoroutine(Dash());
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            return;//冲刺中，跳过物理逻辑
        }

        // 地面检测
        if (groundCheck != null)
        {
            //OverlapCircle：在 groundCheck 脚底位置画一个 0.1 半径圆形，检测是否碰到 ground 层，判断是否落地
            isGrounded = Physics2D.OverlapCircle(groundCheck.position,0.1f,ground);
        }
        //固定物理帧执行：移动、跳跃重力、头顶踩踏敌人检测
        Move();
        Jump();
        Enemy();
    }

    // 移动
    void Move()
    {
        if (Mathf.Abs(x) > 0.01f)//有水平输入（玩家在移动）
        {
            //用定时器控制脚步声播放频率，不再每帧播放
            _footstepTimer += Time.fixedDeltaTime;
            //计时器累加，到达 0.3 秒播放脚步声，重置计时器
            //0.3 秒内，无论跑多少帧，不会重复播放脚步声，效果：走路每 0.3s 响一次脚步声，声音不会叠在一起
            if (_footstepTimer >= FootstepInterval)
            {
                _footstepTimer = 0f;
                AudioManager.Instance?.PlaySound(AudioManager.Instance.footStepSound);
            }
            ani.SetBool("walk", true);
        }
        else
        {
            //停止移动时重置脚步声计时器
            _footstepTimer = 0f;
            ani.SetBool("walk", false);
        }

        // 向右
        if (x > 0.01f &&facingDirection != 1)
        {
            facingDirection = 1;
            //保留 x 缩放的大小，强制变成正数，人物朝右
            //localScale：物体本地缩放，控制大小、翻转。Mathf.Abs()：取绝对值，永远得到正数
            transform.localScale =new Vector3(Mathf.Abs(transform.localScale.x),transform.localScale.y,transform.localScale.z);
        }

        // 向左
        else if (x < -0.01f &&facingDirection != -1)
        {
            facingDirection = -1;
            transform.localScale =new Vector3(-Mathf.Abs(transform.localScale.x),transform.localScale.y,transform.localScale.z);
        }
        //设置刚体速度：X = 输入 × 移动速度；Y 保留原来的竖直速度，不修改竖直方向（跳跃/下落保持原有速度）
        rb.velocity =new Vector2(x * playerSpeed,rb.velocity.y);
    }

    // 跳跃
    void Jump()
    {
        if (isGrounded)
        {
            jumpCount = 2;//如果踩在地面上，重置跳跃次数为 2，恢复二段跳
        }

        if (isJumping)
        {
            //ForceMode2D是枚举，决定施加力的计算方式
            rb.AddForce(Vector2.up * jumpSpeed,ForceMode2D.Impulse);
            isJumping = false;
        }

        // 跳跃重力
        if (rb.velocity.y > 0)
        {
            PPS();
        }

        //如果 Y 速度 < 0（下落） → gravityScale = fallMultiplier=2.5，下落更快
        //如果 Y 速度 > 0（上升）：按住空格 jumpHold = true → gravityScale = 1f，跳得更高。松开空格 jumpHold = false → gravityScale = upMultiplier = 1.5f，上升更快衰减，跳得矮
        //实现：长按空格跳更高，短按跳得低。
        rb.gravityScale =rb.velocity.y < 0? fallMultiplier: (jumpHold? 1f: upMultiplier);
    }

    // 粒子效果
    void PPS()
    {
        if (playerPS != null)
        {
            playerPS.Play();
        }
    }

    // 踩敌人
    // HashSet<Enemy>集合，记录本帧已经踩过的敌人，防止一次踩踏多次触发敌人死亡。HashSet 查找速度比 List 快
    private HashSet<Enemy> _stompedEnemyCache = new HashSet<Enemy>();

    void Enemy()
    {
        //RemoveWhere：HashSet自带方法，删除集合里面满足条件的元素，删除集合里面已经被 Destroy 销毁的敌人（处理 Unity 伪空）
        _stompedEnemyCache.RemoveWhere(x => x == null || !x);
        if (killPoint == null)
        {
            //清空缓存，直接退出
            _stompedEnemyCache.Clear();
            return;
        }

        //在 killPoint 位置画矩形检测盒，只检测 Enemy 层级，拿到敌人碰撞体
        Collider2D enemyCol =Physics2D.OverlapBox(killPoint.position,killPointBoxSize,0f,LayerMask.GetMask("Enemy"));
        if (enemyCol == null)//盒子没有碰到敌人，清空缓存，返回
        {
            _stompedEnemyCache.Clear();
            return;
        }
        //拿到敌人身上 Enemy 脚本；没有 Enemy 脚本，清空缓存返回
        Enemy e = enemyCol.GetComponent<Enemy>();
        if (e == null)
        {
            _stompedEnemyCache.Clear();
            return;
        }
        //使用公开只读属性IsDead，不再直接访问私有isDead字段
        if (e.IsDead)
        {
            _stompedEnemyCache.Clear();
            return;
        }
        // Unity伪空检测，物体已经Destroy
        if (!e)
        {
            _stompedEnemyCache.Clear();
            return;
        }
        //判断是否从上往下踩敌人头顶
        float playerFeetY = killPoint.position.y;
        //.bounds：包围盒，一个结构体，记录这个碰撞体在世界空间的矩形边界
        float enemyTopY = enemyCol.bounds.max.y;
        // 玩家脚底必须高于敌人碰撞盒顶端，才判定为头顶踩踏；0.05是物理浮点容错，解决物理微小误差
        if (playerFeetY <= enemyTopY + 0.05f)
        {
            return;
        }
        // 已经踩过这个敌人，跳过重复伤害
        if (_stompedEnemyCache.Contains(e)) return;

        if (blood != null)
        {
            //Quaternion是四元数，代表物体旋转。Quaternion.identity= 默认旋转，没有旋转，0 度
            Instantiate(blood,transform.position,Quaternion.identity);
        }

        e.Die();
        _stompedEnemyCache.Add(e);
        // 踩敌人后给向上弹跳，马里奥手感，防止粘在敌人尸体上
        rb.velocity = new Vector2(rb.velocity.x, 7f);
    }

    // Dash
    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        //保存原来重力，重力设置 0，冲刺时不受重力，不会下落
        float oldGravity =rb.gravityScale;
        rb.gravityScale = 0f;
        //速度：朝向 × 冲刺力度，Y=0，水平冲刺
        rb.velocity =new Vector2(facingDirection * dashingPower,0f);
        yield return new WaitForSeconds(dashingTime);
        rb.gravityScale =oldGravity;
        isDashing = false;
        yield return new WaitForSeconds(noDashTime);
        canDash = true;
    }

    // 编辑器内绘制红色线框盒子，显示踩踏检测框，只在 Scene 窗口看得见，游戏打包后消失，用来调试
    private void OnDrawGizmos()
    {
        //Gizmos：编辑器调试辅助绘图，只在 Unity Scene 窗口可见，打包发布游戏之后完全消失，玩家看不到
        Gizmos.color = Color.red;
        if (killPoint != null)
        {
            //Gizmos.DrawWireCube(中心点, 尺寸)：画空心线框立方体
            Gizmos.DrawWireCube(killPoint.position,new Vector3(killPointBoxSize.x,killPointBoxSize.y,0f));
        }
    }

    // 保存当前关卡玩家状态
    public void SaveCurrentLevelPlayerState()
    {
        DebugUtil.Log( "【开始执行SaveCurrentLevelPlayerState】");
        SaveGameManager saveManager =SaveGameManager.Instance;

        if (saveManager == null)
        {
            DebugUtil.LogWarning("SaveGameManager.Instance 为空，跳过保存！！！");
            return;
        }

        DebugUtil.Log("SaveGameManager 实例存在");

        PlayerStatus ps =GetComponent<PlayerStatus>();

        if (ps != null)
        {
            DebugUtil.Log($"获取PlayerStatus成功，" +$"isDead={ps.isDead} " +$"isWin={ps.isWin}");

            // 死亡或通关时不能保存
            if (ps.isDead || ps.isWin)
            {
                DebugUtil.Log($"状态：" +$"isDead={ps.isDead} " +$"isWin={ps.isWin} " +$"→ 跳过保存关卡存档");
                return;
            }
        }
        else
        {
            DebugUtil.LogWarning("PlayerStatus 组件找不到！");
        }

        // 当前场景 BuildIndex
        int curLevel =SceneManager.GetActiveScene().buildIndex;

        // 新建存档数据对象，填充数据：玩家 XY 坐标、血量、苹果数量、已吃苹果 ID 列表、击杀敌人 ID 列表
        LevelPlayerSaveData data =new LevelPlayerSaveData();
        data.playerX =transform.position.x;
        data.playerY =transform.position.y;
        data.currentHP =playerHealth != null? playerHealth.currentHP: 4;
        data.appleCount =appleCount;
        data.eatenAppleIds =new List<int>(m_eatenAppleIds);
        data.killedEnemyIds =new List<int>(m_killedEnemyIds);

        //调用存档管理器，把当前关卡数据写入存档，打印日志
        saveManager.SaveLevel(curLevel,data);
        DebugUtil.Log($"【保存成功】" +$"关卡 BuildIndex={curLevel}，" +$"位置X={data.playerX:F2}，" +$"位置Y={data.playerY:F2}，" +$"苹果数量={data.appleCount}");
    }

    // 记录吃掉的苹果
    public void AddEatenAppleId(int id)
    {
        if (!m_eatenAppleIds.Contains(id))
        {
            m_eatenAppleIds.Add(id);
        }
    }

    // 记录击杀的敌人
    public void AddKilledEnemyId(int id)
    {
        if (!m_killedEnemyIds.Contains(id))
        {
            m_killedEnemyIds.Add(id);
        }
    }

    // 根据存档删除已经吃掉的苹果
    //读取存档时执行：找到场景里面所有 PickApple 苹果；如果苹果 ID 在存档已吃列表里面，直接销毁这个苹果，防止读档重复拾取
    private void DestroyEatenApplesFromSave(LevelPlayerSaveData saveData)
    {
        if (saveData == null ||saveData.eatenAppleIds == null)
        {
            return;
        }

        PickApple[] allApples =Object.FindObjectsOfType<PickApple>();

        foreach (var apple in allApples)
        {
            if (saveData.eatenAppleIds.Contains(apple.appleId))
            {
                Destroy(apple.gameObject);
            }
        }
    }

    // 根据存档删除已经击杀的敌人
    private void DestroyKilledEnemiesFromSave(LevelPlayerSaveData saveData)
    {
        if (saveData == null ||saveData.killedEnemyIds == null)
        {
            return;
        }

        Enemy[] allEnemies =Object.FindObjectsOfType<Enemy>();

        foreach (var enemy in allEnemies)
        {
            if (saveData.killedEnemyIds.Contains(enemy.enemyId))
            {
                Destroy(enemy.gameObject);
            }
        }
    }
}
