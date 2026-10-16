using UnityEngine;

//玩家站在移动平台上面的时候，让玩家跟着平台一起移动
//只算平台每一帧移动了多少距离（增量 delta），把这个位移叠加给玩家刚体，玩家自己的 A/D 左右操作还能正常生效
public class PlatformTrigger : MonoBehaviour
{
    private GameObject _playerObj;//站在平台上的玩家物体
    private Vector3 _localOffset;
    //缓存玩家的刚体，物理移动要用刚体，不要直接改transform
    private Rigidbody2D _playerRb;
    //记录上一帧平台的坐标
    private Vector2 _lastPlatformPos;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag(GameConst.Tag_Player))
        {
            _playerObj = collision.gameObject;//保存玩家物体
            _playerRb = _playerObj.GetComponent<Rigidbody2D>();
            // 记录玩家相对平台的偏移，不修改父子
            _localOffset = _playerObj.transform.position - transform.position;
            //玩家刚踏上平台这一刻，记录平台此时此刻的位置，当做基准点
            _lastPlatformPos = transform.position;
        }
    }

    private void FixedUpdate()
    {
        //判断：玩家确实站在平台上，并且刚体不为空
        if (_playerObj != null && _playerRb != null)
        {
            //拿到当前这一帧平台的坐标
            Vector2 currentPlatformPos = transform.position;
            // delta = 当前平台位置 − 上一帧平台位置，就是平台这一帧移动了多少距离（位移增量）
            Vector2 delta = currentPlatformPos - _lastPlatformPos;
            //transform.position只是改了视觉上的位置，但是 Rigidbody2D 自己内部的物理位置没有同步更新
            //Rigidbody2D 有自己一套内部物理坐标，物理引擎做碰撞、速度计算全部看刚体内部数据，不是看transform
            // 把平台位移增量叠加给玩家，不覆盖玩家自身速度
            _playerRb.position += delta;
            //更新基准：把"上一帧位置"更新成本帧平台位置，给下一帧计算用
            _lastPlatformPos = currentPlatformPos;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag(GameConst.Tag_Player))
        {
            if (collision.gameObject == _playerObj)
            {
                //玩家离开平台触发器，清空缓存，不再跟随平台移动
                _playerObj = null;
                _playerRb = null;
            }
        }
    }

    private void OnDestroy()
    {
        _playerObj = null;
        _playerRb = null;
    }
}