using System.Collections;//用来写协程IEnumerator
using UnityEngine;
using Cinemachine;//Cinemachine 虚拟相机，用来做受击屏幕震动效果

public class Enemy : MonoBehaviour
{
    public int health;
    //敌人唯一编号,敌人死亡的时候，把enemyId打包进EnemyDeadEvent事件，发给GameLevelEventHandler，存进玩家KilledEnemyIds列表，用来存档：读档的时候把已经杀死的敌人直接销毁
    public int enemyId;
    [Header("受击震动配置")]
    public CinemachineVirtualCamera vCam;
    public float shakeIntensity = 0.12f;//震动强度，数值越大屏幕抖的越厉害  
    public float shakeDuration = 0.08f;//震动持续多少秒
    [Header("受击闪烁颜色与间隔")]
    public Color hitFlashColor = new Color(1.8f, 0.2f, 0.2f, 1f);
    public float flashOn = 0.07f;//闪红持续时间
    public float flashGap = 0.05f;//两次闪烁中间间隔多久

    private Animator ani;
    private Rigidbody2D rigid;
    private CapsuleCollider2D cc2D;
    private Vector2 lastPosition;//记录敌人上一帧的坐标，用来判断敌人往哪边移动，实现自动翻转面朝方向
    private bool faceRight = false;//标记敌人现在是不是朝右边
    private SpriteRenderer _spriteRenderer;//敌人渲染图片组件，用来修改颜色实现挨打闪红
    private enum enemyState { idle, run, die };
    private bool isDead;//标记敌人是否已经死亡。true=已经死了，防止重复执行死亡逻辑

    //只读属性（箭头 => 简写）
    //别的脚本可以读取enemy.IsDead，只能读，不能写 isDead 私有变量，保护内部状态
    public bool IsDead => isDead;

    private MovingPlatform _movingPlatform;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        ani = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();
        cc2D = GetComponent<CapsuleCollider2D>();
        //transform.position：物体世界坐标，是Vector3类型（x,y,z 三个值）
        //(Vector2)：强制类型转换，把 Vector3 转成Vector2，丢掉 z，只保留 x、y
        lastPosition = (Vector2)transform.position;
        isDead = false;
        _movingPlatform = GetComponent<MovingPlatform>();
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }

    void Update()
    {
        if (isDead) return;
        //如果敌人挂载了 MovingPlatform 组件并且平台现在正在移动 → 敌人状态 = enemyState.run（跑动动画）
        //否则 → 敌人状态 = enemyState.idle（待机动画）
        enemyState enemystates = _movingPlatform != null && _movingPlatform.isMoving ? enemyState.run : enemyState.idle;
        //把状态转成数字，传给 Animator 动画机，切换待机 / 跑动动画
        ani.SetInteger("state", (int)enemystates);
        //根据移动方向，自动翻转敌人左右脸朝向
        UpdateFacingDirection();
    }

    public void takeDamage(int damage)
    {
        if (isDead) return;
        health -= damage;
        //播放打击敌人音效，?.空条件运算符，AudioManager 为空不会报错
        AudioManager.Instance?.PlaySound(AudioManager.Instance.hitEnemySound);
        StartCoroutine(EnemyHitFlashDouble());//启动协程：敌人挨打闪两次红光
        StartCoroutine(EnemyHitShake());//启动协程：相机震动
        if (health <= 0)
        {
            Die();
        }
    }

    //受击闪 2 次红光
    IEnumerator EnemyHitFlashDouble()
    {
        if (_spriteRenderer == null) yield break;
        Color origin = _spriteRenderer.color;//保存敌人原本的颜色
        _spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(flashOn);
        _spriteRenderer.color = origin;
        yield return new WaitForSeconds(flashGap);
        _spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(flashOn);
        _spriteRenderer.color = origin;
    }

    //相机震动
    IEnumerator EnemyHitShake()
    {
        if (vCam == null) yield break;
        //从虚拟相机 vCam 身上，取出T 类型的组件
        //CinemachineBasicMultiChannelPerlin简称 Perlin 噪音组件：Cinemachine 专门用来实现屏幕震动、摇晃的组件
        CinemachineBasicMultiChannelPerlin noise = vCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (noise == null) yield break;
        //把震动强度打开，屏幕开始抖动
        noise.m_AmplitudeGain = shakeIntensity;
        float t = 0;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;//每一帧把 t 加上这一帧消耗的时间
            yield return null;//协程暂停，等到下一帧再回来继续跑 while 循环
        }
        noise.m_AmplitudeGain = 0;
    }


    public void Die()
    {
        if (isDead) return;
        isDead = true;
        ani.SetTrigger("die");
        cc2D.enabled = false;
        // 派发敌人死亡事件，不再直接操作PlayerMovement
        EnemyDeadEvent evt = new EnemyDeadEvent
        {
            enemyId = enemyId
        };
        EventBus.Publish(EventBus.EVENT_ENEMY_DEAD, evt);
        this.enabled = false;//关闭这个脚本,游戏物体还在，只是脚本不跑了，留给死亡动画播放
        Destroy(gameObject, 0.3f);
    }

    //根据移动方向翻转敌人朝向
    private void UpdateFacingDirection()
    {
        //当前位置 - 上一帧位置，算出敌人这一帧移动的方向向量
        Vector2 currentDirection = (Vector2)transform.position - lastPosition;
        if (currentDirection.x > 0 && !faceRight)
        {
            Flip();
        }
        else if (currentDirection.x < 0 && faceRight)
        {
            Flip();
        }
        //更新 lastPosition，保存这一帧坐标，给下一帧计算移动方向
        lastPosition = (Vector2)transform.position;
    }

    private void Flip()
    {
        //读取物体当前的局部缩放`localScale`，存到临时变量 scale
        Vector3 scale = transform.localScale;
        scale.x *= -1;//水平镜像
        transform.localScale = scale;
        faceRight = !faceRight;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.collider != null && collision.collider.CompareTag(GameConst.Tag_DieLine))
        {
            Die();
        }
    }

    //只在编辑器模式生效，打包会删掉这段代码
#if UNITY_EDITOR
    //Inspector 面板修改数值自动触发
    private void OnValidate()
    {
        if (enemyId <= 0) Debug.LogWarning($"{gameObject.name} enemyId 不能小于等于0", this);
    }
#endif
}