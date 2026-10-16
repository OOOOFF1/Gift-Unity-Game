using System.Collections;
using UnityEngine;

//子弹朝指定方向飞行、碰撞检测、击中敌人扣血、超时自动回收回子弹对象池
//不用普通 OnTriggerEnter，而是用CapsuleCast（胶囊射线投射）做碰撞检测
public class Projectile : MonoBehaviour
{
    public float speed = 60f;//子弹飞行速度
    public float lifeTime = 0.5f;//子弹最长存活时间，超过就回收
    public float distance;//额外探测距离（CapsuleCast用）
    public int damage;//子弹伤害值
    public LayerMask layer;//只检测哪些层级（敌人层，忽略玩家、背景等）
    private Vector2 flyDir;//飞行方向（单位向量）
    private Coroutine _lifeCor;//生命周期协程句柄，用来计时回收

    void OnEnable()
    {
        if (_lifeCor != null) StopCoroutine(_lifeCor);//先把旧的生命周期协程停掉，防止上次残留的协程
        _lifeCor = StartCoroutine(LifeCountDown());//开启倒计时协程LifeCountDown：0.5 秒之后自动回收子弹
    }

    private void OnDisable()
    {
        if (_lifeCor != null)
        {
            StopCoroutine(_lifeCor);
            _lifeCor = null;
        }
    }

    public void SetFlyDirection(Vector2 dir)
    {
        flyDir = dir.normalized;//dir.normalized→归一化：把向量长度强制变成 1，只保留方向，去掉距离影响
    }

    private void Update()
    {
        //moveStep：这一帧子弹要移动多少距离。Time.deltaTime上一帧耗时，保证不同帧率移动速度一致
        float moveStep = speed * Time.deltaTime;
        Collider2D col = GetComponent<Collider2D>();
        //拿到子弹身上的碰撞体。如果没有碰撞体，直接回收子弹
        if (col == null)
        {
            ReturnToPool();
            return;
        }

        CapsuleCollider2D capsuleCol = col as CapsuleCollider2D;
        //CapsuleDirection2D.Vertical：竖直胶囊，沿着 Y 轴拉长（上下长，左右窄）
        //CapsuleDirection2D.Horizontal：水平胶囊，沿着 X 轴拉长（左右长，上下窄）
        CapsuleDirection2D realDir = capsuleCol != null ? capsuleCol.direction : CapsuleDirection2D.Vertical;
        //RaycastHit2D是存放碰撞信息的结构体
        //CapsuleCast是一个胶囊实体往前移动一小段，探测路上会不会撞到物体
        RaycastHit2D hitInfo = Physics2D.CapsuleCast(
            transform.position, //胶囊起点位置（子弹当前位置）
            col.bounds.size * 0.5f,//胶囊的尺寸（碰撞体大小一半）
            realDir, //胶囊的轴向方向（水平 / 垂直，和子弹碰撞体保持一致）
            0f,//胶囊旋转角度 0
            flyDir,//投射方向 = 子弹飞行方向
            moveStep + distance,//投射探测距离（这一帧要移动的距离 + 额外预留距离 distance）
            layer//只检测指定层级，别的层直接忽略
        );
        //如果投射碰到东西，hitInfo 不为空，包含碰撞信息
        if (hitInfo)
        {
            if (hitInfo.collider.CompareTag(GameConst.Tag_Enemy))
            {
                Enemy enemy = hitInfo.collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.takeDamage(damage);
                }
            }
            ReturnToPool();//回收子弹，直接 return，不再执行移动
            return;
        }
        //Translate = 移动物体，Space.World：世界坐标系移动，不写 Space.World 默认是 Space.Self（物体自身局部坐标）
        //子弹旋转的时候，如果用自身坐标，飞行方向会错乱；用世界空间，就一直朝 flyDir 这个世界方向飞，不受子弹自身旋转影响
        transform.Translate(flyDir * moveStep, Space.World);
    }

    //子弹飞太远、一直没碰到东西，0.5 秒强制回收，避免子弹无限飞，浪费性能
    IEnumerator LifeCountDown()
    {
        yield return new WaitForSeconds(lifeTime);
        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (_lifeCor != null)
        {
            StopCoroutine(_lifeCor);
            _lifeCor = null;
        }
        BulletObjectPool.Instance?.ReturnBullet(gameObject);
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}
