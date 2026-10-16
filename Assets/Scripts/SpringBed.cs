using System.Collections;
using UnityEngine;

public class SpringBed : MonoBehaviour
{
    public float jumpForce = 10f;
    Animator anim;
    private bool _inCoolDown;//冷却标记：true=冷却中，不能再次触发弹簧

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(GameConst.Tag_Player) && !_inCoolDown)
        {
            anim.SetTrigger("jump");
            //xcollision.gameObject：就是撞到弹簧的物体→玩家
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                //rb.AddForce(向量, ForceMode2D.Impulse)：给玩家施加瞬时冲击力
                //new Vector2(0, jumpForce)：X = 0，Y 方向向上的力
                //Impulse：脉冲力，一次猛撞，瞬间给速度，适合瞬间弹跳（弹簧、跳跃、爆炸）
                rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
            }
            StartCoroutine(SpringCooldown());
        }
    }

    //防止玩家一直踩弹簧，短时间连续触发多次弹跳
    IEnumerator SpringCooldown()
    {
        _inCoolDown = true;//标记：进入冷却，弹簧暂时不能再触发
        yield return new WaitForSeconds(0.3f);//等待0.3秒，这期间代码停在这里
        _inCoolDown = false;//冷却结束，弹簧恢复可用
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}
