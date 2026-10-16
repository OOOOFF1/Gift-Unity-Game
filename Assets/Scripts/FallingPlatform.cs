using System.Collections;//用来写协程 IEnumerator
using System.Collections.Generic;
using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    //玩家踩上去之后，等待多少秒平台才开始往下掉，默认 1 秒
    [SerializeField] private float fallDelay = 1.0f;
    //平台开始下落之后，再等待destroyDelay秒，把这个平台物体彻底销毁
    [SerializeField] private float destroyDelay = 1.0f;
    [SerializeField] private Rigidbody2D rb;
    //这个平台是不是已经开始执行下落流程了，不要再重复启动协程
    private bool _isFalling = false;

    //当别的物体碰到这个平台的碰撞体，这个函数自动执行（必须有 Collider2D 碰撞体，不能是 Trigger）
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(GameConst.Tag_Player))
        {
            //如果_isFalling==true（已经在下落倒计时），直接return，函数直接结束，什么都不干。拒绝重复启动协程
            if (_isFalling)
            {
                return;
            }
            _isFalling = true;

            StartCoroutine(Fall());
        }
    }

    private IEnumerator Fall()
    {
        //暂停在这里，等待 fallDelay（默认 1 秒），这 1 秒游戏照常运行，不会卡死。时间到了，才往下执行后面代码
        yield return new WaitForSeconds(fallDelay);
        // 判空：防止平台已经被销毁，还去操作rb
        if (rb != null)
        {
            //把刚体rb.bodyType从Kinematic（运动学，不受重力）改成Dynamic（动态刚体）
            rb.bodyType = RigidbodyType2D.Dynamic;//可以下落
        }
        Destroy(gameObject, destroyDelay);
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}