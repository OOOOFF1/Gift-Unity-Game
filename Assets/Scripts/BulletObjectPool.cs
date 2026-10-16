using System.Collections.Generic;//用来使用Queue<T>队列容器
using UnityEngine;

//提前预先创建一批子弹，不用的时候把子弹SetActive(false)隐藏起来放回队列；需要用的时候拿出来激活显示
//减少频繁创建销毁物体带来的 GC 垃圾，游戏运行更流畅
public class BulletObjectPool : MonoBehaviour
{
    public static BulletObjectPool Instance;
    public GameObject bulletPrefab;
    public int poolCount = 10;
    //Queue<GameObject>队列，存放空闲、隐藏、可以拿出来用的子弹
    //Enqueue(obj)：放到队伍尾巴（归还子弹）
    //Dequeue()：从队伍头部取子弹（取出子弹使用）
    private Queue<GameObject> bulletQueue;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (bulletPrefab == null)
        {
            Debug.LogError("BulletObjectPool: bulletPrefab 未赋值！");
            bulletQueue = new Queue<GameObject>();
            return;//return直接跳出整个Awake函数,下面代码全部不再执行
        }

        bulletQueue = new Queue<GameObject>();
        for (int i = 0; i < poolCount; i++)
        {
            //Instantiate(bulletPrefab, transform)：实例化子弹预制体；第二个参数transform，让生成出来的子弹作为本对象池物体的子物体
            GameObject obj = Instantiate(bulletPrefab, transform);
            obj.SetActive(false);
            bulletQueue.Enqueue(obj);
        }
    }
    //静态变量不会自动清空，如果不置 null，外部BulletObjectPool.Instance会拿到已经销毁的物体（伪空），调用方法就空引用报错
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public GameObject GetBullet()
    {
        GameObject bullet = null;
        if (bulletQueue.Count > 0)
        {
            bullet = bulletQueue.Dequeue();
        }
        else
        {
            if (bulletPrefab == null)
            {
                Debug.LogError("BulletObjectPool：子弹预制体为空！");
                return null;
            }
            bullet = Instantiate(bulletPrefab, transform);
        }
        if (bullet != null)
            bullet.SetActive(true);
        //把子弹 GameObject 返回出去。FireWeapon.cs拿到子弹，设置位置、旋转、飞行方向
        return bullet;
    }

    //子弹用完归还回池子
    public void ReturnBullet(GameObject bullet)
    {
        if (bullet == null) return;
        bullet.SetActive(false);
        bulletQueue.Enqueue(bullet);
    }
}