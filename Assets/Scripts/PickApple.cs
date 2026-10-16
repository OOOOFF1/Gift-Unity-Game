using UnityEngine;

public class PickApple : MonoBehaviour
{
    [Header("本苹果唯一ID，同一个关卡内不能重复！")]
    public int appleId;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(GameConst.Tag_Player))
        {
            AudioManager.Instance?.PlaySound(AudioManager.Instance.applePickSound);
            //新建事件数据包ApplePickupEvent evt，把当前这个苹果的appleId放进数据包里面
            ApplePickupEvent evt = new ApplePickupEvent
            {
                appleId = appleId
            };
            EventBus.Publish(EventBus.EVENT_APPLE_PICKUP, evt);

            Destroy(gameObject);
        }
    }

    //只要你把苹果 ID 设置成 0 或者负数，控制台弹出黄色警告提醒开发者
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (appleId <= 0)
        {
            Debug.LogWarning($"{gameObject.name} ⚠️ appleId不能<=0，关卡内ID必须唯一", this);
        }
    }
#endif
}
