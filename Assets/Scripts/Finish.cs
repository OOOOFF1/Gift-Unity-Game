using UnityEngine;

public class Finish : MonoBehaviour
{
    //false：还没有到达终点，可以触发通关,true：已经触发过通关了，防止重复触发
    private bool isFinished = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"Finish触发器被碰撞，isFinished={isFinished}，碰撞物体tag={collision.tag}");
        if (isFinished) return;//杜绝重复触发通关事件
        if (collision.CompareTag(GameConst.Tag_Player))
        {
            isFinished = true;
            Debug.Log("触发到达终点事件");
            //新建事件数据包（快递盒子） ReachFinishEvent evt
            ReachFinishEvent evt = new ReachFinishEvent();
            EventBus.Publish(EventBus.EVENT_REACH_FINISH, evt);
        }
    }
}
