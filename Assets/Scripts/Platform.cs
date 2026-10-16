using System.Collections;
using UnityEngine;

public class Platform : MonoBehaviour
{
    //记录玩家当前正站在哪一个单向平台物体上面。没站在平台上就是null
    private GameObject currentPlatform;
    [SerializeField] private CapsuleCollider2D playerCollider;
    //按下 S 之后，忽略碰撞的持续时间 0.5 秒。0.5 秒之后，碰撞恢复正常，玩家又可以踩平台
    [SerializeField] private float recoveryTime = 0.5f;
    //是不是现在正处于忽略碰撞的冷却当中，防止玩家疯狂连按 S，重复启动多个协程
    private bool _isIgnore;

    void Update()
    {
        if (Mathf.Abs(Time.timeScale) < 0.001f)
            return;

        //这是 Unity独有的伪空（fake‑null）问题，C# 普通类没有这个坑，只有继承UnityEngine.Object（GameObject、Component）才有
        //Unity 内部：GameObject 销毁之后，C# 的引用变量不会立刻变成真正的null，而是变成一个 “死掉的包装对象”
        // 修正伪空清理，如果变量看起来不是null
        if (currentPlatform != null)
        {
            // 但是Unity底层发现这个物体实际上已经被销毁不见了
            if (!currentPlatform)
            {
                // 手动把变量赋值成真正的null，后面代码就知道没有平台了
                currentPlatform = null;
            }
        }

        //Input.GetKeyDown("s")：刚刚按下 S 键的瞬间
        //!_isIgnore：现在不在忽略碰撞冷却中（没有正在往下穿）
        //currentPlatform != null：玩家现在确实站在单向平台上面
        if (Input.GetKeyDown("s") && !_isIgnore && currentPlatform != null)
        {
            StartCoroutine(DisableCollision());
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(GameConst.Tag_OneWayPlatform))
        {
            //当玩家碰撞接触到标签为OneWayPlatform单向平台，把这个平台物体记录到currentPlatform
            currentPlatform = collision.gameObject;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(GameConst.Tag_OneWayPlatform))
        {
            currentPlatform = null;
        }
    }

    private IEnumerator DisableCollision()
    {
        _isIgnore = true;//标记：进入冷却，禁止再次按S
        //协程会等待WaitForSeconds这 0.5 秒。在等待期间玩家有可能跳开平台，currentPlatform会被别的代码改成null
        //拷贝到localPlatform，协程就记住触发 S 那一刻的平台，0.5 秒后恢复碰撞的时候，找的是当时那个平台
        GameObject localPlatform = currentPlatform;//把当前平台存到局部变量
        if (localPlatform != null)
        {
            BoxCollider2D platformCollider = localPlatform.GetComponent<BoxCollider2D>();
            // 让玩家碰撞体 和 平台碰撞体互相忽略碰撞
            Physics2D.IgnoreCollision(playerCollider, platformCollider);
            yield return new WaitForSeconds(recoveryTime);//等待0.5秒，玩家落下去
            //第三个参数false：关闭忽略，恢复碰撞
            //Physics2D.IgnoreCollision(碰撞体A,碰撞体B, 是否忽略)
            //不传第三个参数，默认true：A 和 B 互相穿透，不会发生碰撞
            //等待 0.5 秒之后，传入false，恢复碰撞，玩家又可以踩这个平台
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
        _isIgnore = false;//冷却结束，可以再次按S
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (playerCollider == null)
        {
            Debug.LogWarning($"{gameObject.name} ⚠️ Platform脚本需要拖拽赋值 playerCollider", this);
        }
    }
#endif
}
